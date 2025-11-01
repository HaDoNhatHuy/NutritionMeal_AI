using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NutritionWebApp.Services
{
    public class YoutubeDataService : IYoutubeService
    {
        // LƯU Ý: THAY PLACEHOLDER NÀY BẰNG YOUTUBE API KEY CỦA BẠN
        private const string YOUTUBE_API_KEY = "AIzaSyBkIjCWwHcEFl8FuGpZBwqj81vQluSmok4";
        private readonly HttpClient _httpClient;

        public YoutubeDataService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private string? ExtractVideoId(string youtubeUrl)
        {
            if (string.IsNullOrEmpty(youtubeUrl)) return null;

            // Đảm bảo URL hợp lệ để Uri() có thể phân tích
            if (!youtubeUrl.StartsWith("http")) youtubeUrl = "https://" + youtubeUrl;

            try
            {
                var uri = new Uri(youtubeUrl);

                // 1. Xử lý link dạng watch?v=ID (Query string: ?v=...)
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                if (query["v"] != null) return query["v"];

                // 2. Xử lý link dạng /embed/ID (Segments)
                // Ví dụ: https://www.youtube.com/embed/VIDEO_ID
                // Segments sẽ là: {"/", "embed/", "VIDEO_ID"}

                if (uri.Segments.Length >= 3 && uri.Segments[3].ToLower() == "embed/")
                {
                    // Lấy segment thứ 2 (index 2) và loại bỏ dấu '/' cuối cùng (nếu có)
                    string videoId = uri.Segments[2].TrimEnd('/');
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        return videoId;
                    }
                }

                // 3. Xử lý link rút gọn youtu.be/ID (ID là segment cuối cùng)
                if (uri.Host.Contains("youtu.be") && uri.Segments.Length >= 2)
                {
                    // Lấy segment cuối cùng và loại bỏ dấu '/' cuối cùng
                    return uri.Segments.Last().TrimEnd('/');
                }
            }
            catch (Exception)
            {
                // Xử lý nếu URL không thể phân tích
                return null;
            }

            return null;
        }

        public async Task<(string? Title, string? Duration, string? Error)> GetVideoMetadataAsync(string youtubeUrl)
        {
            var videoId = ExtractVideoId(youtubeUrl);
            if (videoId == null)
                return (null, null, "Không trích xuất được Video ID từ URL.");

            // Xây dựng URL API YouTube Data
            string apiUrl = $"https://www.googleapis.com/youtube/v3/videos" +
                            $"?id={videoId}" +
                            $"&key={YOUTUBE_API_KEY}" +
                            $"&part=snippet,contentDetails"; // snippet (Title), contentDetails (Duration)

            try
            {
                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                if (!root.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
                {
                    return (null, null, "Không tìm thấy video trên YouTube hoặc JSON không hợp lệ.");
                }

                //var items = doc.RootElement.GetProperty("items");
                // FIX LỖI: Lấy phần tử ĐẦU TIÊN (là một JSON Object) trong mảng items
                var item = items.EnumerateArray().FirstOrDefault();
                // Kiểm tra item có tồn tại không
                if (item.ValueKind == JsonValueKind.Undefined)
                {
                    return (null, null, "Lỗi phân tích JSON: Phần tử video bị rỗng.");
                }

                //if (items.GetArrayLength() == 0)
                //    return (null, null, "Không tìm thấy video trên YouTube.");

                //var item = items;
                var snippet = item.GetProperty("snippet");
                var contentDetails = item.GetProperty("contentDetails");

                string title = snippet.GetProperty("title").GetString()!;
                string durationIso = contentDetails.GetProperty("duration").GetString()!;

                // Ở đây bạn có thể thêm logic chuyển đổi ISO 8601 Duration (VD: PT10M30S)
                // thành định dạng dễ đọc hơn (VD: 10 phút 30 giây) nếu cần.
                // Tạm thời trả về chuỗi ISO
                return (title, durationIso, null);

            }
            catch (Exception ex)
            {
                return (null, null, $"Lỗi gọi API YouTube: {ex.Message}");
            }
        }
    }
}