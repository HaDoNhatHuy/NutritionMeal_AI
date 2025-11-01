using System;
using System.Collections.Generic;
using NutritionWebApp.Models.Entities;

namespace NutritionWebApp.ViewModels
{
    public class ChatSessionSummary
    {
        // Sử dụng Ngày (Date) làm định danh cho Session
        public DateTime SessionDate { get; set; }

        // Tiêu đề của Session (Ví dụ: "Trò chuyện ngày 15/11")
        public string Title { get; set; }

        // Nội dung tin nhắn cuối cùng (preview)
        public string LastMessagePreview { get; set; }

        // Số lượng tin nhắn trong phiên
        public int MessageCount { get; set; }
    }
}