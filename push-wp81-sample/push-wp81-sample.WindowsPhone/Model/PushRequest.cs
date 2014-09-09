using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace push_wp81_sample.Model
{

    [DataContract]
    class PushRequest
    {
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public PushRequestMessage Message { get; set; }

        [DataMember(Name = "target", EmitDefaultValue = false)]
        public PushRequestTarget Target { get; set; }

        private PushRequest()
        {
        }

        public static PushRequest MakePushRequest(string body, string[] devices, string templateType, string templateName, Dictionary<string, string> templateFields)
        {
            return new PushRequest
            {
                Message = new PushRequestMessage
                {
                    Body = body,
                    Custom = new PushRequestMessageCustom
                    {
                        Windows8 = new PushRequestMessageCustomWindows8
                        {
                            Type = templateType,
                            TemplateName = templateName,
                            TemplateFields = templateFields
                        }
                    }
                },

                Target = new PushRequestTarget
                {
                    Platform = "windows8",
                    Devices = devices
                }
            };
        }
    }

    [DataContract]
    internal class PushRequestMessage
    {
        [DataMember(Name = "body", EmitDefaultValue = false)]
        public string Body { get; set; }

        [DataMember(Name = "custom", EmitDefaultValue = false)]        
        public PushRequestMessageCustom Custom { get; set; }
    }

    [DataContract]
    internal class PushRequestMessageCustom
    {
        [DataMember(Name = "windows8", EmitDefaultValue = false)]
        public PushRequestMessageCustomWindows8 Windows8 { get; set; }
    }

    internal class PushRequestMessageCustomWindows8
    {
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        [DataMember(Name = "template_name", EmitDefaultValue = false)]
        public string TemplateName { get; set; }

        [DataMember(Name = "template_fields", EmitDefaultValue = false)]
        public Dictionary<String, String> TemplateFields { get; set; } 
    }

    [DataContract]
    internal class PushRequestTarget
    {
        [DataMember(Name = "platform", EmitDefaultValue = false)]
        public string Platform { get; set; }

        [DataMember(Name = "devices", EmitDefaultValue = false)]
        public string[] Devices { get; set; }
    }
}
