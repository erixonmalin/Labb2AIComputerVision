using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using System.Drawing;
using System.Net;

namespace Labb2AIComputerVision
{
    public class Program
    {
        private static ComputerVisionClient cvClient;

        static async Task Main(string[] args)
        {
            string end;

            try
            {
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
                string cogSvcKey = configuration["CognitiveServiceKey"];

                // Computer Vision client
                ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
                cvClient = new ComputerVisionClient(credentials)
                {
                    Endpoint = cogSvcEndpoint
                };

                do
                {
                    Console.Write("Enter URL to your picture: ");
                    string imageUrl = Console.ReadLine();

                    await AnalyzeImage(imageUrl);

                    await GetThumbnail(imageUrl);

                    Console.Write("Hit Enter to continue or write 'exit' to Exit: ");
                    end = Console.ReadLine();

                } while (end.ToLower() != "exit");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            static async Task AnalyzeImage(string imageUrl)
            {
                Console.WriteLine($"Analyzing {imageUrl}");

                List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
                {
                    VisualFeatureTypes.Description,
                    VisualFeatureTypes.Categories,
                    VisualFeatureTypes.Tags,
                    VisualFeatureTypes.Brands,
                    VisualFeatureTypes.Adult,
                    VisualFeatureTypes.Color,
                    VisualFeatureTypes.Objects,
                };

                ImageAnalysis analysis = await cvClient.AnalyzeImageAsync(imageUrl, features);

                // Get Image Captions
                foreach (var caption in analysis.Description.Captions)
                {
                    Console.WriteLine($"Description: {caption.Text} (confidence: {caption.Confidence.ToString("P")})");
                }

                // Get Categories
                List<LandmarksModel> landmarks = new List<LandmarksModel> { };
                Console.WriteLine("Categories:");
                foreach (var category in analysis.Categories)
                {
                    Console.WriteLine($" -{category.Name} (confidence: {category.Score.ToString("P")})");

                    // Get landmarks in this category
                    if (category.Detail?.Landmarks != null)
                    {
                        foreach (LandmarksModel landmark in category.Detail.Landmarks)
                        {
                            if (!landmarks.Any(item => item.Name == landmark.Name))
                            {
                                landmarks.Add(landmark);
                            }
                        }
                    }
                }

                // List of landmarks
                if (landmarks.Count > 0)
                {
                    Console.WriteLine("Landmarks:");
                    foreach (LandmarksModel landmark in landmarks)
                    {
                        Console.WriteLine($" -{landmark.Name} (confidence: {landmark.Confidence.ToString("P")})");
                    }
                }

                // Get Tags
                if (analysis.Tags.Count > 0)
                {
                    Console.WriteLine("Tags:");
                    foreach (var tag in analysis.Tags)
                    {
                        Console.WriteLine($" -{tag.Name} (confidence: {tag.Confidence.ToString("P")})");
                    }
                }

                // Get Brands
                if (analysis.Brands.Count > 0)
                {
                    Console.WriteLine("Brands:");
                    foreach (var brand in analysis.Brands)
                    {
                        Console.WriteLine($" -{brand.Name} (confidence: {brand.Confidence.ToString("P")})");
                    }
                }

                // Get Adult content
                if (analysis.Adult.IsAdultContent || analysis.Adult.IsRacyContent)
                {
                    if (analysis.Adult.IsAdultContent)
                    {
                        Console.WriteLine("Adult content detected.");
                    }
                    if (analysis.Adult.IsRacyContent)
                    {
                        Console.WriteLine("Provocative content detected.");
                    }
                }

                // Moderation ratings
                string ratings = $"Ratings:\n -Adult: {analysis.Adult.IsAdultContent}\n -Racy: {analysis.Adult.IsRacyContent}\n -Gore: {analysis.Adult.IsGoryContent}";
                Console.WriteLine(ratings);

                // Get Color - Accent
                if (analysis.Color.AccentColor != null)
                {
                    var accentColor = analysis.Color.AccentColor;
                    Console.Write("Accent color: " + accentColor);
                }

                // Get Color - Dominant
                if (analysis.Color.DominantColors != null)
                {
                    Console.WriteLine("Dominant Color:");
                    foreach (var color in analysis.Color.DominantColors)
                    {
                        Console.WriteLine($" -{color}/n");
                    }
                }

                // Get objects in the image
                if (analysis.Objects.Count > 0)
                {
                    Console.WriteLine("Objects in image:");

                    foreach (var obj in analysis.Objects)
                    {
                        Console.WriteLine($"{obj.ObjectProperty} with confidence {obj.Confidence} at location {obj.Rectangle}, " +
                                          $"{obj.Rectangle.X + obj.Rectangle.W}, {obj.Rectangle.Y}, {obj.Rectangle.Y + obj.Rectangle.H}");
                    }

                    WebClient wc = new WebClient();
                    byte[] bytes = wc.DownloadData(imageUrl);
                    MemoryStream ms = new MemoryStream(bytes);

                    System.Drawing.Image image = System.Drawing.Image.FromStream(ms);
                    Graphics graphics = Graphics.FromImage(image);
                    Pen pen = new Pen(Color.Cyan, 3);
                    Font font = new Font("Arial", 16);
                    SolidBrush brush = new SolidBrush(Color.Black);

                    foreach (var detectedObject in analysis.Objects)
                    {
                        // Print Object name
                        Console.WriteLine($" -{detectedObject.ObjectProperty} (confidence: {detectedObject.Confidence.ToString("P")})");

                        // Draw Object bounding box
                        var r = detectedObject.Rectangle;
                        Rectangle rect = new Rectangle(r.X, r.Y, r.W, r.H);
                        graphics.DrawRectangle(pen, rect);
                        graphics.DrawString(detectedObject.ObjectProperty, font, brush, r.X, r.Y);

                    }

                    // Save annotated image
                    String output_file = "objects.jpg";
                    image.Save(output_file);
                    Console.WriteLine("Results saved in " + output_file);
                }
            }

            static async Task GetThumbnail(string imageUrl)
            {
                Console.WriteLine("Generating thumbnail");

                // Get thumbnail data
                var thumbnailStream = await cvClient.GenerateThumbnailAsync(100, 100, imageUrl, true);

                // Save thumbnail image
                string thumbnailFileName = "thumbnail.png";
                using (Stream thumbnailFile = File.Create(thumbnailFileName))
                {
                    thumbnailStream.CopyTo(thumbnailFile);
                }

                Console.WriteLine($"Thumbnail saved in {thumbnailFileName}\n");
            }
        }
    }
}