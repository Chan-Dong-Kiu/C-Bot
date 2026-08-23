using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FPTEnglishRAG.Application.Abstractions;
using FPTEnglishRAG.Domain.Entities;
using FPTEnglishRAG.Domain.Enums;
using FPTEnglishRAG.Domain.ValueObjects;
using FPTEnglishRAG.Wpf.ViewModels;
using FPTEnglishRAG.Wpf.Views;
using Moq;

namespace FPTEnglishRAG.UnitTests.ViewModels;

public class ChatViewRenderTest
{
    private static void EnsureApplicationResources()
    {
        if (System.Windows.Application.Current == null)
        {
            var app = new FPTEnglishRAG.Wpf.App();
            app.InitializeComponent();
        }
    }

    [Fact]
    public void RenderAndSaveScreenshots()
    {
        Exception? testEx = null;
        var thread = new Thread(() =>
        {
            try
            {
                EnsureApplicationResources();

                var mockChatService = new Mock<IChatService>();
                var mockSessionStore = new Mock<IChatSessionStore>();
                var session = new ChatSession();
                mockSessionStore.Setup(s => s.GetOrCreateActiveSession()).Returns(session);

                var vm = new ChatViewModel(mockChatService.Object, mockSessionStore.Object);

                var view = new ChatView
                {
                    DataContext = vm,
                    Width = 1000,
                    Height = 700
                };

                view.Measure(new Size(1000, 700));
                view.Arrange(new Rect(0, 0, 1000, 700));
                view.UpdateLayout();

                string artifactDir = @"C:\Users\Thu Nguyen\.gemini\antigravity-ide\brain\a2afc1be-3a22-4868-994c-6d8b6f1decc3";
                Directory.CreateDirectory(artifactDir);

                // 1. Screenshot empty state
                SaveControlImage(view, Path.Combine(artifactDir, "screenshot_empty_chat.png"), 1000, 700);

                // 2. Add messages to active chat
                var userMsg = new ChatMessage(Guid.NewGuid(), ChatRole.User, "sunday tiếng anh nghĩa là gì", DateTimeOffset.UtcNow, ChatMessageStatus.Completed, Array.Empty<Citation>());
                vm.Messages.Add(new ChatMessageViewModel(userMsg));

                var assistantContent = "General Gemini knowledge (not from imported documents).\n\nTrong tiếng Anh, **\"Sunday\"** có nghĩa là **Chủ Nhật** (ngày cuối tuần).\n\nMột số thông tin bổ sung hữu ích khi học từ này:\n- **Phiên âm:** /ˈsʌn.deɪ/ hoặc /ˈsʌn.di/\n- **Từ viết tắt:** Sun.\n- **Giới từ đi kèm:** Sử dụng giới từ **\"on\"** trước Sunday (ví dụ: *on Sunday*, *on Sundays*).\n- **Ví dụ:**\n  - *I usually play football on Sunday.* (Tôi thường chơi bóng đá vào Chủ Nhật.)\n  - *See you next Sunday!* (Hẹn gặp lại bạn vào Chủ Nhật tuần tới!)";
                var assistantMsg = new ChatMessage(Guid.NewGuid(), ChatRole.Assistant, assistantContent, DateTimeOffset.UtcNow, ChatMessageStatus.Completed, Array.Empty<Citation>());
                vm.Messages.Add(new ChatMessageViewModel(assistantMsg));

                view.Measure(new Size(1000, 700));
                view.Arrange(new Rect(0, 0, 1000, 700));
                view.UpdateLayout();

                // Screenshot active chat
                SaveControlImage(view, Path.Combine(artifactDir, "screenshot_active_chat.png"), 1000, 700);
            }
            catch (Exception ex)
            {
                testEx = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (testEx != null)
        {
            throw new InvalidOperationException($"Render test failed: {testEx.Message}", testEx);
        }
    }

    private static void SaveControlImage(FrameworkElement element, string filePath, int width, int height)
    {
        var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(element);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        encoder.Save(stream);
    }
}
