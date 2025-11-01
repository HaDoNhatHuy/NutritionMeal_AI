namespace NutritionWebApp.Services
{
    public interface IYoutubeService
    {
        // Hàm này sẽ lấy Title và Duration từ URL/ID của YouTube
        Task<(string? Title, string? Duration, string? Error)> GetVideoMetadataAsync(string youtubeUrl);
    }
}
