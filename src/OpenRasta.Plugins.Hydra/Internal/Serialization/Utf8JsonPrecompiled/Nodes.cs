using Utf8Json;

namespace OpenRasta.Plugins.Hydra.Internal.Serialization.Utf8JsonPrecompiled
{
  static class Nodes
  {
    public static byte[] BeginObjectContext;
//    public static byte[] ContextSuffixQuoteComa;
    public static byte[] IdProperty;
//    static byte[] TypeProperty;

    static Nodes()
    {
      var writer = new JsonWriter();
      writer.WriteBeginObject();
      writer.WritePropertyName("@context");
//      writer.WriteQuotation();
      BeginObjectContext = writer.ToUtf8ByteArray();
      
      
      writer = new JsonWriter();
      writer.WritePropertyName("@id");
      IdProperty = writer.ToUtf8ByteArray();
    }
    
  }
}