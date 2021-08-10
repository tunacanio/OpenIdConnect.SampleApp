using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenIdConnect.SampleApp.Models.Account
{
    public class OidcStateSerializer : IDataSerializer<OidcState>
    {
        [return: MaybeNull]
        public OidcState Deserialize(byte[] data)
        {
            using var memory = new MemoryStream(data);
            using var reader = new BinaryReader(memory);
            return Read(reader);
        }

        public byte[] Serialize(OidcState model)
        {
            using var memory = new MemoryStream();
            using var writer = new BinaryWriter(memory);
            Write(writer, model);
            writer.Flush();

            return memory.ToArray();
        }

        public void Write(BinaryWriter writer, OidcState data)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (data == null) throw new ArgumentNullException(nameof(data));

            //writer.Write("CodeVerifierCacheId");
            writer.Write(data.CodeVerifierCacheId);

            //writer.Write("CodeChallenge");
            writer.Write(data.CodeChallenge);
        }

        public OidcState Read(BinaryReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            //var count = reader.ReadInt32();

            var data = new OidcState();

            data.CodeVerifierCacheId = reader.ReadString();
            data.CodeChallenge = reader.ReadString();

            return data;
        }
    }
}
