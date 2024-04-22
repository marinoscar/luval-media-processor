using luval.mp.Metadata;
using System.Diagnostics;

namespace luval.mp.tests
{
    public class When_Reading_Photo_Metadata
    {
        [Fact]
        public void It_Should_Extract_All()
        {
            var result = MediaMetadataReader.FromFile("img/sample-jpg-01.jpg");
            foreach (var item in result.ExtendedProperties)
            {
                Debug.WriteLine($"name: {item.Key} value: {item.Value}");
            }
        }
    }
}