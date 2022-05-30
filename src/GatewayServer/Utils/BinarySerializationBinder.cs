using GatewayServer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace GatewayServer.Utils
{
    public class BinarySerializationBinder : SerializationBinder
    {
        public override Type? BindToType(string assemblyName, string typeName)
        {
            if (typeName.Equals("GatewayServer.Services.IotHubSender"))
                return typeof(IoTHubSender);
            return null;
        }

        private static byte[] SerializeData(IoTHubSender obj)
        {
            var binaryFormatter = new BinaryFormatter();
            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        private static IoTHubSender DeserializeData(byte[] bytes)
        {
            var binaryFormatter = new BinaryFormatter
            {
                Binder = new BinarySerializationBinder()
            };

            using (var memoryStream = new MemoryStream(bytes))
                return (IoTHubSender)binaryFormatter.Deserialize(memoryStream);
        }
    }

}
