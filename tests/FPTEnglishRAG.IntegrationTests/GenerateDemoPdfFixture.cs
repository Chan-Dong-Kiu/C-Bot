using System.IO;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;
using Xunit;

namespace FPTEnglishRAG.IntegrationTests;

public class GenerateDemoPdfFixture
{
    [Fact]
    public void GenerateSamplePdf()
    {
        var targetFile = Path.Combine(Path.GetTempPath(), $"fpt-english-{Guid.NewGuid():N}.pdf");

        try
        {
            var builder = new PdfDocumentBuilder();

            // Page 1
            var page1 = builder.AddPage(PageSize.A4);
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);
            var fontBold = builder.AddStandard14Font(Standard14Font.HelveticaBold);

            page1.AddText("FPT UNIVERSITY - ENGLISH ENTRY ASSESSMENT GUIDE", 14, new PdfPoint(50, 750), fontBold);
            page1.AddText("Chapter 1: Essential Grammar & Sentence Transformation", 12, new PdfPoint(50, 720), fontBold);
            page1.AddText("1. Present Simple vs Present Continuous:", 11, new PdfPoint(50, 690), fontBold);
            page1.AddText("The Present Simple expresses general truths, habits, and permanent situations.", 10, new PdfPoint(50, 670), font);
            page1.AddText("Example: Students take the placement test before starting their academic major.", 10, new PdfPoint(50, 655), font);
            page1.AddText("The Present Continuous describes actions happening at the moment of speaking.", 10, new PdfPoint(50, 635), font);
            page1.AddText("Example: The professor is currently explaining the IELTS grading rubric.", 10, new PdfPoint(50, 620), font);

            // Page 2
            var page2 = builder.AddPage(PageSize.A4);
            page2.AddText("Chapter 2: Reading Strategies and Vocabulary in Context", 12, new PdfPoint(50, 750), fontBold);
            page2.AddText("When answering multiple choice reading questions, identify keywords in the question stem.", 10, new PdfPoint(50, 720), font);
            page2.AddText("Look for synonyms and paraphrases in the passage rather than exact word matching.", 10, new PdfPoint(50, 700), font);
            page2.AddText("Context clues such as contrasting conjunctions (however, although) help infer meaning.", 10, new PdfPoint(50, 680), font);

            var bytes = builder.Build();
            File.WriteAllBytes(targetFile, bytes);

            Assert.True(File.Exists(targetFile));
            Assert.NotEmpty(File.ReadAllBytes(targetFile));
        }
        finally
        {
            File.Delete(targetFile);
        }
    }
}
