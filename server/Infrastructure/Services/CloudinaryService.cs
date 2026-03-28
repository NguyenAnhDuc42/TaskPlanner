public class CloudinaryService 
{
    // private readonly Cloudinary _cloudinary;

    // public CloudinaryService(Cloudinary cloudinary)
    // {
    //     _cloudinary = cloudinary;
    // }

    // public async Task<ImageUploadResult> UploadFileAsync(IFormFile file)
    // {
    //     using var stream = file.OpenReadStream();
    //     var uploadParams = new ImageUploadParams
    //     {
    //         File = new FileDescription(file.FileName, stream),
    //     };
    //     var result = await _cloudinary.UploadAsync(uploadParams);
    //     return result.SecureUrl;
    // }
    // public async Task<string> UploadVideoAsync(IFormFile file)
    // {
    //     using var stream = file.OpenReadStream();
    //     var uploadParams = new VideoUploadParams
    //     {
    //         File = new FileDescription(file.FileName, stream),
    //     };
    //     var result = await _cloudinary.UploadAsync(uploadParams);
    //     return result.SecureUrl;
    // }
}