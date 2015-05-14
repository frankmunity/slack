﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tab.Slack.Common.Model.Events;
using Tab.Slack.Common.Model.Events.Messages;
using Tab.Slack.Common.Model.Responses;

namespace Tab.Slack.Common.Json
{
    public class ResponseParser : IResponseParser
    {
        private readonly JsonSerializer jsonSerializer;
        private readonly JsonSerializerSettings serializerSettings;

        public ResponseParser()
        {
            this.serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new LowerCaseDelimitedPropertyNamesContractResolver('_'),
                Converters = new List<JsonConverter> { new UnderscoreEnumTypeConverter() }
            };

            this.jsonSerializer = JsonSerializer.Create(this.serializerSettings);
        }

        public string SerializeMessage(object message)
        {
            return JsonConvert.SerializeObject(message, Formatting.None, this.serializerSettings);
        }

        public IEnumerable<MessageBase> RemapMessagesToConcreteTypes(IEnumerable<MessageBase> messages)
        {
            if (messages == null)
                throw new ArgumentNullException(nameof(messages));

            foreach (var baseMessage in messages)
            {
                var concreteMessage = ParseEvent<MessageSubType>(
                                        baseMessage.UnmatchedAdditionalJsonData, 
                                        baseMessage.Subtype.ToString().ToDelimitedString('_')
                                      ) as MessageBase;

                concreteMessage.Channel = baseMessage.Channel;
                concreteMessage.Subtype = baseMessage.Subtype;
                concreteMessage.Team = baseMessage.Team;
                concreteMessage.Ts = baseMessage.Ts;
                concreteMessage.Type = baseMessage.Type;
                concreteMessage.Text = baseMessage.Text;
                concreteMessage.User = baseMessage.User;

                yield return concreteMessage;
            }
        }

        public T Deserialize<T>(string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            
            return JsonConvert.DeserializeObject<T>(content, this.serializerSettings);
        }

        public EventMessageBase DeserializeEvent(string content)
        {
            var jsonObject = JsonConvert.DeserializeObject(content) as JObject;
            EventMessageBase eventMessage = null;

            if ((string)jsonObject["type"] == "message")
                eventMessage = ParseEvent<MessageSubType>(jsonObject, (string)jsonObject["subtype"] ?? "plain_message");
            else
                eventMessage = ParseEvent<EventType>(jsonObject, (string)jsonObject["type"]);

            return eventMessage;
        }

        private EventMessageBase ParseEvent<T>(JObject jsonObject, string typeKey)
        {
            var enumValue = UnderscoreEnumTypeConverter.FindMatchingName<T>(typeKey);
            var typeName = typeof(T).AssemblyQualifiedName.Replace(typeof(T).Name, enumValue);

            return (jsonObject ?? new JObject()).ToObject(Type.GetType(typeName), this.jsonSerializer) as EventMessageBase;
        }
    }
}
