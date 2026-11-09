using System.Text;

namespace RaceDay.Api.Services;

/// <summary>
/// Checks an uploaded file really is an image by reading its first bytes ("magic numbers"),
/// instead of trusting the file name or the content type sent by the browser.
/// </summary>
public static class ImageFileValidator
{
    public const long MaxBytes = 5 * 1024 * 1024;

    public record Result(string? Error, string Extension, string ContentType);

    public static async Task<Result> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return Invalid("Please choose an image file to upload.");
        }

        if (file.Length > MaxBytes)
        {
            return Invalid("The image must be 5 MB or smaller.");
        }

        var header = new byte[12];
        int read;
        await using (var stream = file.OpenReadStream())
        {
            read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
        }

        if (read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return new Result(null, ".jpg", "image/jpeg");
        }

        if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            return new Result(null, ".png", "image/png");
        }

        if (read >= 4 && Encoding.ASCII.GetString(header, 0, 4) == "GIF8")
        {
            return new Result(null, ".gif", "image/gif");
        }

        if (read >= 12 && Encoding.ASCII.GetString(header, 0, 4) == "RIFF" && Encoding.ASCII.GetString(header, 8, 4) == "WEBP")
        {
            return new Result(null, ".webp", "image/webp");
        }

        return Invalid("Only JPEG, PNG, WebP or GIF images are allowed.");
    }

    private static Result Invalid(string error) => new(error, string.Empty, string.Empty);
}
