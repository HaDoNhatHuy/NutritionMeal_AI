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

        public string? ExtractVideoId(string youtubeUrl)
        {
            if (string.IsNullOrEmpty(youtubeUrl)) return null;

            // Đảm bảo URL hợp lệ
            if (!youtubeUrl.StartsWith("http")) youtubeUrl = "https://" + youtubeUrl;

            try
            {
                var uri = new Uri(youtubeUrl);
                string? videoIdWithQuery = null;

                // 1. Xử lý link dạng watch?v=ID
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                if (query["v"] != null) return query["v"];

                // 2. Xử lý link dạng /embed/ID (Segments)
                // Segment 0 là /; Segment 1 là embed/; Segment 2 là ID
                if (uri.Segments.Length >= 3 && uri.Segments[1].ToLower() == "embed/")
                {
                    // FIX LỖI CÚ PHÁP: Lấy phần tử chuỗi ở index 2, sau đó mới TrimEnd
                    videoIdWithQuery = uri.Segments[2].TrimEnd('/');
                }

                // 3. Xử lý link rút gọn youtu.be/ID (ID là segment cuối cùng)
                else if (uri.Host.Contains("youtu.be") && uri.Segments.Length >= 2)
                {
                    // FIX LỖI CÚ PHÁP: Lấy phần tử chuỗi cuối cùng, sau đó mới TrimEnd
                    videoIdWithQuery = uri.Segments.Last().TrimEnd('/');
                }

                // --- CHUẨN HÓA ID: LOẠI BỎ CÁC THAM SỐ TRUY VẤN (?si=...) ---
                if (videoIdWithQuery != null)
                {
                    int qIndex = videoIdWithQuery.IndexOf('?');
                    if (qIndex != -1)
                    {
                        // Chỉ lấy phần trước dấu '?'
                        return videoIdWithQuery.Substring(0, qIndex);
                    }
                    return videoIdWithQuery; // Trả về ID nếu không có query string
                }
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }
        private string FormatDuration(string isoDuration)
        {
            // Sử dụng XmlConvert để chuyển đổi chuỗi ISO 8601 thành TimeSpan
            TimeSpan duration;
            try
            {
                // PT10M30S -> TimeSpan
                duration = System.Xml.XmlConvert.ToTimeSpan(isoDuration);
            }
            catch
            {
                return "Không xác định"; // Trả về mặc định nếu chuỗi không hợp lệ
            }

            string formatted = "";
            if (duration.Hours > 0)
            {
                formatted += $"{duration.Hours} giờ ";
            }
            if (duration.Minutes > 0)
            {
                formatted += $"{duration.Minutes} phút ";
            }
            // Chỉ hiển thị giây nếu không có giờ/phút, hoặc nếu nó là video siêu ngắn
            if (duration.Seconds > 0 || formatted == "")
            {
                formatted += $"{duration.Seconds} giây";
            }

            return formatted.Trim();
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
                // GỌI HÀM CHUYỂN ĐỔI MỚI
                string formattedDuration = FormatDuration(durationIso);

                // Ở đây bạn có thể thêm logic chuyển đổi ISO 8601 Duration (VD: PT10M30S)
                // thành định dạng dễ đọc hơn (VD: 10 phút 30 giây) nếu cần.
                // Tạm thời trả về chuỗi ISO
                return (title, formattedDuration, null);

            }
            catch (Exception ex)
            {
                return (null, null, $"Lỗi gọi API YouTube: {ex.Message}");
            }
        }
    }
}