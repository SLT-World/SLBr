/*Copyright © SLT Softwares. All rights reserved.
Use of this source code is governed by a GNU license that can be found in the LICENSE file.*/

namespace SLBr.Protobuf
{
    public static class SafeBrowsingDownloadParser
    {
        public struct ClientDownloadResponse
        {
            public int Verdict;
        }

        public static ClientDownloadResponse ParseResponse(ReadOnlySpan<byte> Buffer)
        {
            ClientDownloadResponse _FullHash = new();
            ProtobufReader Reader = new(Buffer);
            while (!Reader.IsConsumed)
            {
                if (!Reader.TryReadTag(out int FieldNumber, out int WireType))
                    break;
                if (WireType == 0 && FieldNumber == 1)
                {
                    _FullHash.Verdict = Reader.ReadVarint();
                    break;
                }
                //else
                    //Reader.SkipField(WireType);
            }
            return _FullHash;
        }
    }
}
