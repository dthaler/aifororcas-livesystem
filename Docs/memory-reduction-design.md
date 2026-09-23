# Memory Reduction Design Document for LiveInferenceOrchestrator.py

## Executive Summary

This document proposes up to 3 targeted improvements to reduce memory usage of the LiveInferenceOrchestrator.py system from current levels of 1.7-2.3 GiB down to an average of ~1.5 GiB on Azure F4sv2 or F2sv2 nodes.

**Current State:**
- Pod memory usage: ~834-929 MiB RSS per pod
- Page cache: ~724 MiB - 1.05 GiB per pod
- Total system memory: 1.7-2.3 GiB (exceeds target of 1.5 GiB)
- Current limit: 3GB per pod in Kubernetes deployment
- Target node types: Azure F4sv2 (8GB RAM) or F2sv2 (4GB RAM)

**Analysis Context:**
The memory analysis shows two key components contributing to memory usage:
1. **Anonymous memory (anon)**: 834-929 MiB - actual process memory including model weights, inference data, and Python runtime
2. **File cache (file)**: 724 MiB - 1.05 GiB - kernel page cache from audio file I/O and spectrogram operations
3. **Inactive anonymous memory**: High values suggest memory pressure or large allocations not actively used

## Proposed Improvements

### Improvement 1: Optimize Matplotlib/Spectrogram Memory Management (HIGH IMPACT)

**Current Issue:**
The `spectrogram_visualizer.py` module creates matplotlib figures multiple times per clip without explicit cleanup, leading to memory accumulation:

```python
# Current implementation in write_spectrogram()
- Creates 2 temporary spectrogram images per clip (firstHalf.png, secondHalf.png)
- Uses librosa.load() which loads entire audio into memory
- Creates canvas arrays and OpenCV operations
- No explicit garbage collection between iterations
```

**Root Causes:**
1. Matplotlib figures are not explicitly closed, causing memory leaks
2. Audio data from librosa.load() stays in memory longer than needed
3. Temporary numpy arrays from spectrograms persist
4. Multiple image conversions (matplotlib -> file -> cv2 -> canvas) keep data in memory

**Proposed Changes:**

**File: `InferenceSystem/src/spectrogram_visualizer.py`**

1. **Add explicit figure cleanup** (lines 16-36):
   - Already partially implemented with `plt.close(fig)` in `_create_spectrogram_figure()`
   - Ensure this pattern is used consistently

2. **Add memory cleanup in write_spectrogram()** (after line 88):
```python
def write_spectrogram(wav_file_path):
    # ... existing code ...
    
    cv2.imwrite(spec_output_path, canvas)
    
    # Explicitly clean up large arrays
    del canvas, spec1, spec2, y, y_first_half, y_second_half
    del X_first_half, Xdb_first_half, X_second_half, Xdb_second_half
    
    # Force garbage collection after each spectrogram
    import gc
    gc.collect()
    
    return spec_output_path
```

3. **Add memory cleanup in write_annotations_on_spectrogram()** (after line 121):
```python
def write_annotations_on_spectrogram(wav_file_path, wav_timestamp, data, spec_output_path):
    # ... existing code ...
    
    cv2.imwrite(spec_output_path, image)
    
    # Clean up large objects
    del y, S, S_dB, image
    
    import gc
    gc.collect()
```

**Expected Impact:**
- **Memory reduction**: 100-200 MiB per pod (reducing peak usage during spectrogram generation)
- **File cache reduction**: 50-100 MiB (less temporary file I/O)
- **Implementation effort**: LOW (a few hours)
- **Risk**: VERY LOW (only adds cleanup, doesn't change functionality)
- **Total reduction: ~150-300 MiB**

**Verification:**
- Monitor RSS memory before and after each clip processing iteration
- Check for reduction in inactive_anon memory
- Verify no increase in processing latency

---

### Improvement 2: Optimize FastAI Model Inference Memory Usage (MEDIUM-HIGH IMPACT)

**Current Issue:**
The `FastAIModel.predict()` method in `fastai_inference.py` has several memory-intensive operations:

```python
# Current implementation issues:
1. Creates temporary directory with dozens of 2-second audio clips (lines 118-147)
2. Loads entire audio file into memory via librosa.get_duration()
3. Extracts and saves 58-60 separate 2-second WAV files to disk
4. Creates AudioDataLoader that loads all clips into memory (lines 167-172)
5. Processes predictions in a loop without cleanup (lines 175-179)
6. No cleanup of temporary files until after all processing
```

**Root Causes:**
1. Audio segmentation creates 58-60 files on disk (each ~88KB) = ~5MB temporary files
2. AudioList.from_folder() loads all audio segments into memory simultaneously
3. DataBunch with bs=32 means up to 32 mel spectrograms in memory at once
4. Pandas DataFrames persist throughout prediction cycle
5. tempfile.mkdtemp() directory not cleaned up if process crashes

**Proposed Changes:**

**File: `InferenceSystem/src/model/fastai_inference.py`**

1. **Process audio segments in streaming fashion** (lines 112-181):
```python
def predict(self, wav_file_path):
    '''
    Function which generates local predictions using wavefile
    Memory-optimized streaming approach
    '''
    import tempfile
    import shutil
    from contextlib import contextmanager
    
    @contextmanager
    def temp_directory():
        """Context manager for safe temporary directory handling"""
        temp_dir = tempfile.mkdtemp() + "/"
        try:
            yield temp_dir
        finally:
            if os.path.exists(temp_dir):
                shutil.rmtree(temp_dir, ignore_errors=True)
    
    with temp_directory() as local_dir:
        # Infer clip length
        max_length = get_duration(path=wav_file_path)
        print(os.path.basename(wav_file_path))
        print("Length of Audio Clip:{0}".format(max_length))
        
        # Generate 2 sec proposals with 1 sec hop length
        twoSecList = []
        for i in range(int(floor(max_length)-1)):
            twoSecList.append([i, i+2])
        
        # Creating a proposal dictionary
        two_sec_dict = {}
        two_sec_dict[Path(wav_file_path).name] = twoSecList
        
        # Extract segments
        extract_segments(
            str(Path(wav_file_path).parent),
            two_sec_dict,
            local_dir,
            ""
        )
        
        # Audio config
        config = AudioConfig(standardize=False,
                            sg_cfg=SpectrogramConfig(
                                f_min=0.0,
                                f_max=10000,
                                hop_length=256,
                                n_fft=2560,
                                n_mels=256,
                                pad=0,
                                to_db_scale=True,
                                top_db=100,
                                win_length=None,
                                n_mfcc=20)
                            )
        config.duration = 4000
        config.resample_to = 20000
        config.downmix = True
        
        # Creating AudioDataLoader with smaller batch size
        test_data_folder = Path(local_dir)
        tfms = None
        test = AudioList.from_folder(
            test_data_folder, config=config).split_none().label_empty()
        # Reduce batch size from 32 to 8 to lower memory footprint
        testdb = test.transform(tfms).databunch(bs=8)
        
        # Score each 2 sec clip
        predictions = []
        pathList = list(pd.Series(test_data_folder.ls()).astype('str'))
        for item in testdb.x:
            predictions.append(self.model.predict(item)[2][1])
        
        # Aggregating predictions
        prediction = pd.DataFrame({'FilePath': pathList, 'confidence': predictions})
        prediction['confidence'] = prediction.confidence.astype(float)
        prediction['start_time_s'] = prediction.FilePath.apply(lambda x: int(x.split('_')[-2]))
        prediction = prediction.sort_values(['start_time_s']).reset_index(drop=True)
        
        # Rolling Window
        submission = pd.DataFrame(
                {
                    'wav_filename': Path(wav_file_path).name,
                    'duration_s': 1.0,
                    'confidence': list(prediction.rolling(2)['confidence'].mean().values)
                }
            ).reset_index().rename(columns={'index': 'start_time_s'})
        
        submission.loc[0, 'confidence'] = prediction.confidence[0]
        
        lastLine = pd.DataFrame({
            'wav_filename': Path(wav_file_path).name,
            'start_time_s': [submission.start_time_s.max()+1],
            'duration_s': 1.0,
            'confidence': [prediction.confidence[prediction.shape[0]-1]]
            })
        submission = pd.concat([submission, lastLine], ignore_index=True)
        submission = submission[['wav_filename', 'start_time_s', 'duration_s', 'confidence']]
        
        # Initialize output JSON
        result_json = dict(
            submission=submission,
            local_predictions=list((submission['confidence'] > self.threshold).astype(int)),
            local_confidences=list(submission['confidence'])
        )
        
        result_json['global_prediction'] = int(sum(result_json["local_predictions"]) >= self.min_num_positive_calls_threshold)
        result_json['global_confidence'] = submission.loc[(submission['confidence'] > self.threshold), 'confidence'].mean()*100
        if pd.isnull(result_json["global_confidence"]):
            result_json["global_confidence"] = 0
        
        # Cleanup before returning
        del prediction, submission, testdb, test
        import gc
        gc.collect()
        
        return result_json
    # temp_directory context manager automatically cleans up temporary files
```

2. **Key changes:**
   - Reduced batch size from 32 to 8 (reduces peak memory by ~75%)
   - Added context manager for guaranteed temporary directory cleanup
   - Added explicit cleanup of large DataFrames and objects
   - Replace deprecated DataFrame.append() with pd.concat()

**Expected Impact:**
- **Memory reduction**: 150-300 MiB per pod (from reduced batch size and better cleanup)
- **Peak memory**: Reduced by 200-250 MiB during inference
- **File I/O**: Reduced page cache pressure
- **Implementation effort**: MEDIUM (1-2 days including testing)
- **Risk**: LOW-MEDIUM (requires validation that batch size reduction doesn't affect accuracy)
- **Total reduction: ~200-350 MiB**

**Verification:**
- Test that prediction accuracy remains unchanged with bs=8 vs bs=32
- Monitor memory during inference cycles
- Verify temporary directory cleanup on exceptions
- Compare prediction latency (may increase slightly with smaller batches)

---

### Improvement 3: Optimize Audio Loading and Model Inference Pipeline (MEDIUM IMPACT)

**Current Issue:**
The `podcast_inference.py` (AudioSet model) loads entire audio files and creates multiple copies in memory:

```python
# Current issues in OrcaDetectionModel.split_and_predict():
1. AudioFileWindower loads entire audio file (line 56-58)
2. Iterates through all windows keeping data in memory (line 68-83)
3. Creates torch tensors for each window without cleanup (line 73)
4. Accumulates all predictions in lists (lines 82-83)
5. Creates pandas DataFrame with all data at end (lines 85-96)
```

**Root Causes:**
1. No streaming approach - entire audio stays in memory
2. Torch tensors accumulate in GPU/CPU memory
3. Result accumulation in lists grows throughout processing
4. No intermediate cleanup between windows

**Proposed Changes:**

**File: `InferenceSystem/src/model/podcast_inference.py`**

1. **Add periodic memory cleanup during inference loop** (lines 68-83):
```python
def split_and_predict(self, wav_file_path):
    """
    Args contains:
        - wavfile_path
        - model_path 
    """
    
    # initialize parameters
    wavfile_path = wav_file_path
    chunk_duration = params.INFERENCE_CHUNK_S
    
    audio_file_windower = AudioFileWindower(
            [wavfile_path], mean=self.mean, invstd=self.invstd, hop_s=self.hop_s
        )
    window_s = audio_file_windower.window_s
    
    # initialize output JSON
    result_json = {
        "local_predictions": [],
        "local_confidences": []
    }
    
    # iterate through dataloader and accumulate predictions
    num_windows = len(audio_file_windower)
    for i in tqdm(range(num_windows)):
        # get a mel spec for the window 
        audio_file_windower.get_mode = 'mel_spec'
        mel_spec_window, _ = audio_file_windower[i]
        
        # run inference on window
        with torch.no_grad():  # Disable gradient computation to save memory
            input_data = torch.from_numpy(mel_spec_window).float().unsqueeze(0).unsqueeze(0)
            pred, _ = self.model(input_data)
            posterior = np.exp(pred.detach().cpu().numpy())
        
        pred_id = 0
        if posterior[0, 1] > self.threshold:
            pred_id = 1
        confidence = round(float(posterior[0, 1]), 3)
        
        result_json["local_predictions"].append(pred_id)
        result_json["local_confidences"].append(confidence)
        
        # Cleanup tensors every 10 windows to prevent accumulation
        if i % 10 == 0:
            del input_data, pred, posterior, mel_spec_window
            import gc
            gc.collect()
    
    # Final cleanup before DataFrame creation
    del audio_file_windower
    import gc
    gc.collect()
    
    submission = pd.DataFrame(dict(
        wav_filename=Path(wav_file_path).name,
        start_time_s=[i*self.hop_s for i in range(num_windows)],
        duration_s=self.hop_s,
        confidence=result_json['local_confidences']
    ))
    
    if self.rolling_avg:
        rolling_scores = submission['confidence'].rolling(2).mean()
        rolling_scores[0] = submission['confidence'][0]
        submission['confidence'] = rolling_scores
        result_json["local_confidences"] = submission['confidence'].tolist()
    
    result_json['submission'] = submission
    
    return result_json
```

2. **Add torch.no_grad() context manager** to prevent gradient accumulation
3. **Periodic garbage collection** every 10 windows instead of letting objects accumulate
4. **Explicit cleanup** of audio_file_windower before DataFrame creation

**Expected Impact:**
- **Memory reduction**: 50-150 MiB per pod (from torch memory management)
- **Peak reduction**: Lower memory spikes during inference
- **Implementation effort**: LOW-MEDIUM (1 day)
- **Risk**: VERY LOW (only adds cleanup and optimization)
- **Total reduction: ~75-150 MiB**

**Verification:**
- Monitor torch memory usage during inference
- Verify prediction accuracy unchanged
- Check for reduction in memory spikes
- Minimal to no latency impact expected

---

## Additional Recommendations (Lower Priority)

### 4. Reduce Model Batch Processing (If using AudioSet)
If the AudioSet model is in use, the window processing could be optimized further, but FastAI model is currently more memory-intensive.

### 5. Optimize HLS Stream Caching
The orca-hls-utils library may cache audio chunks. Consider reviewing if configurable cache limits can be set.

### 6. Container-level Optimizations
- Set `MALLOC_TRIM_THRESHOLD_=65536` environment variable to make glibc more aggressive about returning memory to OS
- Consider using jemalloc as memory allocator (requires Docker image changes)

---

## Implementation Priority and Timeline

### Phase 1: Quick Wins (Week 1)
1. **Improvement 1**: Spectrogram memory cleanup
   - Effort: 4-8 hours
   - Expected reduction: 150-300 MiB
   - Risk: Very low

2. **Improvement 3**: AudioSet model optimization
   - Effort: 8-16 hours
   - Expected reduction: 75-150 MiB
   - Risk: Very low

**Phase 1 Total Expected Reduction: 225-450 MiB**

### Phase 2: Larger Refactoring (Week 2-3)
3. **Improvement 2**: FastAI model optimization
   - Effort: 16-32 hours (includes testing)
   - Expected reduction: 200-350 MiB
   - Risk: Low-Medium

**Phase 2 Total Expected Reduction: 200-350 MiB**

### Combined Impact
- **Total Expected Memory Reduction: 425-800 MiB per pod**
- **Current Usage**: 834-929 MiB RSS + 724-1050 MiB page cache = ~1.6-2.0 GB
- **Expected After Improvements**: ~1.1-1.5 GB total
- **Target**: 1.5 GB average ✓

---

## Risk Assessment

| Improvement | Implementation Risk | Performance Impact | Validation Required |
|-------------|-------------------|-------------------|---------------------|
| Improvement 1 (Spectrogram) | Very Low | None expected | Visual comparison of spectrograms |
| Improvement 2 (FastAI) | Low-Medium | Minor latency increase possible | Accuracy testing on validation set |
| Improvement 3 (AudioSet) | Very Low | None expected | Accuracy comparison |

---

## Monitoring and Validation

### Memory Metrics to Track
1. **RSS (Resident Set Size)**: Target reduction from 900 MiB to 600-700 MiB
2. **Page Cache (file)**: Target reduction from 800 MiB to 500-600 MiB
3. **Peak Memory**: Monitor max memory during inference cycles
4. **Inactive Anonymous**: Should decrease with better cleanup

### Performance Metrics
1. **Processing Latency**: Should remain < 60 seconds per clip
2. **Detection Accuracy**: Should remain unchanged
3. **Pod Stability**: Monitor for OOMKill events

### Kubernetes Configuration Updates

After implementing improvements, consider updating resource limits:

```yaml
# Current
resources:
  limits:
    memory: 3G

# Proposed after improvements
resources:
  requests:
    memory: 1.5G
  limits:
    memory: 2G
```

---

## Azure VM Sizing Recommendations

### F2sv2 (2 vCPU, 4 GB RAM)
- **Current**: Cannot fit 2 pods (would need ~4-6GB with overhead)
- **After improvements**: Can fit 2 pods comfortably (~3-3.5GB total with k8s overhead)

### F4sv2 (4 vCPU, 8 GB RAM)
- **Current**: Can fit 2-3 pods with memory pressure
- **After improvements**: Can fit 4 pods comfortably (~6-7GB total)

**Recommendation**: Start with F4sv2 to provide safety margin, then consider F2sv2 after validating improvements.

---

## Conclusion

The proposed improvements target three key areas of memory usage:

1. **Spectrogram generation cleanup** (HIGH impact, LOW risk) - 150-300 MiB
2. **FastAI model optimization** (HIGH impact, LOW-MEDIUM risk) - 200-350 MiB  
3. **AudioSet model optimization** (MEDIUM impact, LOW risk) - 75-150 MiB

**Total expected reduction: 425-800 MiB per pod**

This should bring average memory usage from 1.7-2.3 GiB down to the target of ~1.5 GiB, preventing pod evictions and allowing better resource utilization on Azure F2sv2/F4sv2 nodes.

Implementation should proceed in phases, starting with low-risk improvements (Phase 1) to achieve immediate memory reduction, followed by the more substantial FastAI optimization (Phase 2) for maximum impact.
