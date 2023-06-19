using System.Collections.Generic;

public static class ImageDataStorage
{
    public static List<ImageData> imageDataList = new List<ImageData>();

    public class ImageData
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string InferenceId { get; set; }
    }
}

public class InferencesResponse
{
    public List<Inference> inferences { get; set; }
    public string nextPaginationToken { get; set; }
}

public class Inference
{
    public string id { get; set; }
    public List<Image> images { get; set; }
}

public class Image
{
    public string id { get; set; }
    public string url { get; set; }
}

public class TokenResponse
{
    public string nextPaginationToken { get; set; }
}