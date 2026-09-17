using AIForOrcas.Client.Web.Models;
using AIForOrcas.DTO.API;

namespace AIForOrcas.Client.Web.Pages.Detections;

public partial class SingleDetection : ComponentBase, IDisposable
{
    [Parameter]
    public string Id { get; set; }

    [Inject]
    IJSRuntime JSRuntime { get; set; }

    [Inject]
    IDetectionService Service { get; set; }

    [Inject]
    IToastService ToastService { get; set; }

    [Inject]
    UserTagCache TagCache { get; set; }

    [Inject]
    AuthenticationStateProvider AuthenticationStateProvider { get; set; }

    private string _userId;
    private Detection detection = null;
    private DetectionMinute detectionMinute = null;
    private bool isFound = true;
    private bool isUnavailable = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadDetection();

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        _userId = user.FindFirst("oid")?.Value;
    }

    private async Task LoadDetection()
    {
        detection = await Service.GetDetectionAsync(Id);
        isUnavailable = detection == null;
        if (!isUnavailable && detection.Id == null)
        {
            isFound = false;
            return;
        }

        // If we have a valid detection, fetch all detections that share the same minute and location.
        if (!isUnavailable)
        {
            // Compute minute start/end for the detection timestamp preserving Kind.
            var ts = detection.Timestamp;
            var minuteStart = new DateTime(ts.Year, ts.Month, ts.Day, ts.Hour, ts.Minute, 0, ts.Kind);
            var minuteEnd = minuteStart.AddMinutes(1);

            // Build pagination and filter options to hit the root GET endpoint with a date range and location filter.
            var pagination = new PaginationOptionsDTO
            {
                Page = 1,
                RecordsPerPage = 50
            };

            var filter = new ReviewedFilterOptionsDTO
            {
                SortBy = "timestamp",
                SortOrder = "desc",
                Timeframe = "range",
                Location = "all",
                HydrophoneId = string.IsNullOrWhiteSpace(detection.Location?.Id) ? "all" : detection.Location.Id,
                DateFrom = minuteStart,
                DateTo = minuteEnd
            };

            var result = await Service.GetDetectionsAsync(pagination, filter);
            var allDetections = result?.Response ?? new List<Detection>();

            // Initialize detectionMinute using the returned detection set. The factory groups by minute+location,
            // so the requested set should produce one group; pick the first.
            var minutes = DetectionMinute.CreateDetectionMinutes(allDetections);
            detectionMinute = minutes.FirstOrDefault() ?? new DetectionMinute { Detections = new List<Detection> { detection } };
        }
    }

    private async Task ActOnSubmitCallback(DetectionUpdate request)
    {
        await Service.UpdateRequestAsync(request);

        List<string> leafTags = Detection.GetLeafTags(request.Tags);
        TagCache.SetTags(_userId, leafTags);

        ToastService.ShowSuccess("Detection successfully updated.");

        await LoadDetection();
    }

    void IDisposable.Dispose()
    {
        JSRuntime.InvokeVoidAsync("DestroyActivePlayer");
    }
}
