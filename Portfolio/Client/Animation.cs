using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Portfolio.Client
    {
    public class Animation
        {
        public Size KeyFrameCanvasSize { get; set; }
        public List<KeyFrame> KeyFrames { get; set; }
        }
    public class KeyFrame
        {
        [JsonConverter(typeof(RectangleJsonConverter))]
        public Rectangle Rectangle { get; set; }
        public int FrameNumber { get; set; }
        }

    // Rectange doesn't need to write out every single public property.
    // X, Y, Width, Height is sufficient.
    public class RectangleJsonConverter : JsonConverter<Rectangle>
        {
        public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
            Rectangle rectangle = new();

            String propertyName = "";
            bool done = false;
            while (!done && reader.Read())
                {
                switch (reader.TokenType)
                    {
                    case JsonTokenType.PropertyName:
                        propertyName = reader.GetString();
                        break;
                    case JsonTokenType.Number:
                        switch (propertyName)
                            {
                            case "X":
                                rectangle.X = reader.GetInt32();
                                break;
                            case "Y":
                                rectangle.Y = reader.GetInt32();
                                break;
                            case "Width":
                                rectangle.Width = reader.GetInt32();
                                break;
                            case "Height":
                                rectangle.Height = reader.GetInt32();
                                break;
                            }
                        break;
                    case JsonTokenType.EndObject:
                        done = true;
                        break;
                    }
                }

            return rectangle;
            }

        public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
            {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteNumber("Width", value.Width);
            writer.WriteNumber("Height", value.Height);
            writer.WriteEndObject();
            }
        }
    }
