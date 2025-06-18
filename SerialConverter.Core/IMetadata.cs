using System.ComponentModel;

namespace SerialConverter.Core
{
    public interface IMetadataExtension
    {
        [DefaultValue("")]
        string? Guid { get; set; }

        [DefaultValue(0)]
        int Priority { get; set; }

        [DefaultValue("Null")]
        string? Name { get; set; }

        [DefaultValue("Null")]
        string? LongName {  get; set; }
        
        [DefaultValue("Null")]
        string? Description { get; set; }

        [DefaultValue("Nulls")]
        string? Image { get; set; }
        
        [DefaultValue("Null")]
        string? Author { get; set; }
        
        [DefaultValue("1.0.0")]
        string? Version { get; set; }
    }

    public class MetadataExtensionAttribute : Attribute, IMetadataExtension
    {
        public string? Guid { get; set; }
        public int Priority { get; set; }
        public string? Name { get; set; }
        public string? LongName { get; set; }
        public string? Description { get; set; }
        public string? Image { get; set; }
        public string? Author { get; set; }
        public string? Version { get; set; }
    }
}
