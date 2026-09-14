using System;
using System.Collections.Generic;
using System.Linq;

namespace AIForOrcas.Client.Web.Models
{
    public class DetectionMinute
    {
        public List<Detection> Detections { get; set; }

        // Properties common to all Detections in the minute. These are derived from the first detection in the list.
        public string AudioUri => Detections?.FirstOrDefault()?.AudioUri;
        public string SpectrogramUri => Detections?.FirstOrDefault()?.SpectrogramUri;
        public Location Location => Detections?.FirstOrDefault()?.Location;
        public DateTime Timestamp => Detections?.FirstOrDefault()?.Timestamp ?? default;
        public bool Reviewed => Detections?.FirstOrDefault()?.Reviewed ?? false;
        public DateTime Moderated => Detections?.FirstOrDefault()?.Moderated ?? default;

        public string Moderator
        {
            get
            {
                if (Detections == null || Detections.Count == 0)
                {
                    return string.Empty;
                }

                var models = Detections
                    .Where(d => !string.IsNullOrWhiteSpace(d?.Moderator))
                    .Select(d => d.Moderator.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return string.Join(",", models);
            }
        }

        public string AIModel
        {
            get
            {
                if (Detections == null || Detections.Count == 0)
                {
                    return string.Empty;
                }

                var models = Detections
                    .Where(d => !string.IsNullOrWhiteSpace(d?.AIModel))
                    .Select(d => d.AIModel.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return string.Join(", ", models);
            }
        }

        // TODO: should remove this when practical.
        public string Id => Detections?.FirstOrDefault()?.Id ?? default;

        public List<Annotation> Annotations
        {
            get
            {
                if (Detections == null || Detections.Count == 0)
                {
                    return new List<Annotation>();
                }

                // Combine all Annotations from the underlying detections into a single list
                // and sort them by StartTime (then EndTime) so the merged list is ordered.
                var combined = Detections
                    .Where(d => d?.Annotations != null)
                    .SelectMany(d => d.Annotations.Select(a => new Annotation
                    {
                        Id = a.Id,
                        StartTime = a.StartTime,
                        EndTime = a.EndTime,
                        Confidence = a.Confidence,
                        Label = a.Label
                    }))
                    .OrderBy(a => a.StartTime)
                    .ThenBy(a => a.EndTime)
                    .ToList();

                return combined;
            }
        }

        public string Found
        {
            get { return Detections?.FirstOrDefault()?.Found; }
            set
            {
                if (Detections == null) return;
                foreach (var d in Detections)
                {
                    d.Found = value;
                }
            }
        }
        public string Comments
        {
            get
            {
                return Detections?.FirstOrDefault()?.Comments;
            }
            set
            {
                foreach (var d in Detections)
                {
                    d.Comments = value;
                }
            }
        }
        public List<string> TagList
        {
            get
            {
                if (Detections == null || Detections.Count == 0)
                {
                    return new List<string>();
                }

                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var d in Detections)
                {
                    if (string.IsNullOrWhiteSpace(d?.Tags))
                    {
                        continue;
                    }

                    foreach (var p in Detection.GetTagList(d.Tags))
                    {
                        var tag = p.Trim();
                        if (!string.IsNullOrEmpty(tag))
                        {
                            set.Add(tag);
                        }
                    }
                }

                return set.ToList();
            }
        }
        public string Tags
        {
            get
            {
                return string.Join(";", TagList);
            }
            set
            {
                foreach (var d in Detections)
                {
                    d.Tags = value;
                }
            }
        }
        public List<string> SuggestedTagList
        {
            get
            {
                var suggestions = new List<string>();

                // For each tag in the tags list, add any child tags not already in the tags list.
                var tagList = TagList;
                foreach (var tag in tagList)
                {
                    foreach (var pair in Detection.TagHierarchy)
                    {
                        if (tag.Equals(pair.Value, StringComparison.OrdinalIgnoreCase) && !tagList.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            suggestions.Add(pair.Key);
                        }
                    }
                }

                // Add any top-level tags not already in the tags list.
                foreach (var pair in Detection.TagHierarchy)
                {
                    if (pair.Value == null && !tagList.Contains(pair.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        suggestions.Add(pair.Key);
                    }
                }

                return suggestions;
            }
        }
        public string GlobalPredictionLabel => Detections?.FirstOrDefault()?.GlobalPredictionLabel;

        public static List<DetectionMinute> CreateDetectionMinutes(List<Detection> detections)
        {
            var detectionMinutes = new List<DetectionMinute>();
            if (detections == null || detections.Count == 0)
            {
                return detectionMinutes;
            }

            // Group by minute and by location so each group represents a single timestamp/location pair.
            var groupedDetections = detections
                .GroupBy(d => new
                {
                    Timestamp = d.Timestamp.ToString("yyyy-MM-dd HH:mm"),
                    LocationName = d.Location?.Name ?? string.Empty
                });

            foreach (var group in groupedDetections)
            {
                detectionMinutes.Add(new DetectionMinute
                {
                    Detections = group.ToList()
                });
            }
            return detectionMinutes;
        }
    }
}
