using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TomatoRadar.Utils
{
    static internal class KorabliReplayReader
    {
        private static readonly byte[] ReplayHeader = { 0x12, 0x32, 0x34, 0x11 };

        public static JObject? ReadKorabliReplay(string filePath)
        {
            JObject? result = TryReadReplayBlocksJSON(filePath);
            if (result != null)
                return result;

            result = ReadArenaInfoEntities(filePath);
            return result;
        }

        private static JObject? TryReadReplayBlocksJSON(string filePath)
        {
            byte[]? data = TryReadReplayFile(filePath);
            if (data == null)
                return null;

            int offset = 8;
            uint blockCount = BitConverter.ToUInt32(data, 4);

            ReadOnlySpan<byte> metaBlock = ReadBlock(data, ref offset);
            if (metaBlock.Length == 0)
                return null;

            string metaJson = Encoding.UTF8.GetString(metaBlock);
            JObject metaObj = JObject.Parse(metaJson);

            string matchGroup = metaObj["matchGroup"]?.Value<string>() ?? string.Empty;
            string dateTime = metaObj["dateTime"]?.Value<string>() ?? string.Empty;
            string playerName = metaObj["playerName"]?.Value<string>() ?? string.Empty;
            int isFogOfWar = metaObj["isFogOfWar"]?.Value<int>() ?? 0;

            LogUtils.WriteInfo($"KorabliReplay [replay header]: meta={matchGroup}, player={playerName}, isFogOfWar={isFogOfWar}");

            JArray vehiclesArray = new();

            if (blockCount > 1)
            {
                ReadOnlySpan<byte> block1Data = ReadBlock(data, ref offset);
                string block1Json = Encoding.UTF8.GetString(block1Data);
                try
                {
                    JObject block1Obj = JObject.Parse(block1Json);
                    JObject? ppi = block1Obj["playersPublicInfo"] as JObject;
                    if (ppi != null && ppi.Count > 0)
                    {
                        int currentTeam = -1;
                        var players = new List<(string name, string shipId, int teamId)>();
                        foreach (var prop in ppi.Properties())
                        {
                            JArray? arr = prop.Value as JArray;
                            if (arr == null || arr.Count < 8)
                                continue;
                            string name = arr[1]!.Value<string>()!;
                            string shipId = arr[7]!.Value<long>().ToString();
                            int teamId = arr[6]!.Value<int>();
                            players.Add((name, shipId, teamId));
                            if (string.Equals(name, playerName, StringComparison.OrdinalIgnoreCase))
                                currentTeam = teamId;
                        }

                        int idCounter = 100;
                        foreach (var p in players)
                        {
                            int relation = (p.teamId == currentTeam) ? 1 : 8;
                            vehiclesArray.Add(new JObject
                            {
                                ["name"] = p.name,
                                ["shipId"] = p.shipId,
                                ["relation"] = relation,
                                ["id"] = idCounter++,
                            });
                        }
                        LogUtils.WriteInfo($"KorabliReplay [playersPublicInfo]: {vehiclesArray.Count} players");
                    }
                }
                catch (Exception ex)
                {
                    LogUtils.WriteInfo($"Block 1 JSON parse failed: {ex.Message}");
                }
            }

            if (vehiclesArray.Count == 0)
                return null;

            return new JObject
            {
                ["vehicles"] = vehiclesArray,
                ["matchGroup"] = matchGroup,
                ["dateTime"] = dateTime,
                ["isFogOfWar"] = isFogOfWar,
            };
        }

        private static JObject? ReadArenaInfoEntities(string filePath)
        {
            byte[] data;
            using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (fs.Length < 4)
                    return null;
                data = new byte[fs.Length];
                int total = 0;
                while (total < data.Length)
                {
                    int read = fs.Read(data, total, data.Length - total);
                    if (read == 0) break;
                    total += read;
                }
            }

            var decompBlocks = new List<byte[]>();
            for (int i = 0; i < data.Length - 1; i++)
            {
                if (data[i] == 0x78 && (data[i + 1] == 0x9C || data[i + 1] == 0xDA || data[i + 1] == 0x01))
                {
                    byte[]? decomp = TryDecompress(data, i);
                    if (decomp != null && decomp.Length > 50)
                        decompBlocks.Add(decomp);
                }
            }

            if (decompBlocks.Count == 0)
            {
                LogUtils.WriteInfo("KorabliReplay [arena info]: no zlib blocks found");
                return null;
            }

            var entities = new List<(string name, string displayName, ulong shipId, int teamId)>();

            foreach (var blk in decompBlocks)
            {
                ParseMessagePackEntities(blk, entities);
            }

            string? currentPlayerName = null;
            int? observedTeam = null;
            foreach (var blk in decompBlocks)
            {
                int? ot = TryExtractObservedTeam(blk);
                if (ot.HasValue)
                    observedTeam = ot;
            }

            if (entities.Count == 0)
            {
                LogUtils.WriteInfo("KorabliReplay [arena info]: no entities extracted");
                return null;
            }

            foreach (var e in entities)
            {
                if (!e.displayName.StartsWith(":"))
                {
                    currentPlayerName = e.displayName;
                    break;
                }
            }

            int currentTeam;
            if (observedTeam.HasValue)
            {
                currentTeam = observedTeam.Value;
            }
            else
            {
                int team0Count = 0, team1Count = 0;
                foreach (var e in entities)
                {
                    if (e.teamId == 0) team0Count++;
                    if (e.teamId == 1) team1Count++;
                }
                if (team0Count > 0 || team1Count > 0)
                    currentTeam = team0Count <= team1Count ? 0 : 1;
                else
                    currentTeam = 1;
            }

            var vehiclesArray = new JArray();
            int idCounter = 100;

            LogUtils.WriteInfo($"KorabliReplay [arena info]: player={currentPlayerName}, team={currentTeam}");

            foreach (var e in entities)
            {
                int teamId = e.teamId;
                if (teamId < 0)
                    teamId = currentTeam;

                int relation = (teamId == currentTeam) ? 1 : 8;

                vehiclesArray.Add(new JObject
                {
                    ["name"] = e.displayName,
                    ["shipId"] = e.shipId.ToString(),
                    ["relation"] = relation,
                    ["id"] = idCounter++,
                });
            }

            string matchGroup = "battle";
            string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

            LogUtils.WriteInfo($"KorabliReplay [arena info]: extracted {vehiclesArray.Count} vehicles");

            return new JObject
            {
                ["vehicles"] = vehiclesArray,
                ["matchGroup"] = matchGroup,
                ["dateTime"] = dateTime,
            };
        }

        private static void ParseMessagePackEntities(
            byte[] data,
            List<(string name, string displayName, ulong shipId, int teamId)> entities)
        {
            var entityRegions = new List<(int start, int end)>();

            for (int i = 0; i <= data.Length - 6; i++)
            {
                bool isBoundary = false;

                if (data[i] == 0x92 && data[i + 1] == 0x00 && data[i + 2] == 0xCE && i + 7 <= data.Length)
                {
                    uint acctId = (uint)(data[i + 3] << 24 | data[i + 4] << 16 | data[i + 5] << 8 | data[i + 6]);
                    if (acctId > 100)
                        isBoundary = true;
                }

                if (data[i] == 0x92 && data[i + 1] == 0x00 && data[i + 2] == 0xD2 && i + 7 <= data.Length)
                {
                    int acctId = data[i + 3] << 24 | data[i + 4] << 16 | data[i + 5] << 8 | data[i + 6];
                    if (acctId < 0)
                        isBoundary = true;
                }

                if (isBoundary)
                    entityRegions.Add((i, int.MaxValue));
            }

            if (entityRegions.Count == 0)
            {
                entityRegions.Add((0, data.Length));
            }
            else
            {
                for (int ei = 0; ei < entityRegions.Count - 1; ei++)
                    entityRegions[ei] = (entityRegions[ei].start, entityRegions[ei + 1].start);
                entityRegions[entityRegions.Count - 1] = (entityRegions[entityRegions.Count - 1].start, data.Length);
            }

            LogUtils.WriteDebug($"ParseMessagePack: {data.Length}B block → {entityRegions.Count} entity regions");

            foreach (var (entityStart, entityEnd) in entityRegions)
            {
                string? name = null;
                ulong? shipId = null;
                int? teamId = null;

                for (int j = entityStart; j < entityEnd - 2; j++)
                {
                    if (data[j] != 0x92 || data[j + 1] > 0x7F)
                        continue;

                    int propId = data[j + 1];
                    int valOff = j + 2;
                    if (valOff >= entityEnd)
                        break;

                    if (propId == 0x1C && valOff + 1 < data.Length && data[valOff] == 0xC4 && valOff + 2 + data[valOff + 1] <= data.Length)
                    {
                        int slen = data[valOff + 1];
                        if (slen >= 2 && slen <= 35)
                            name = Encoding.ASCII.GetString(data, valOff + 2, slen);
                    }
                    else if (propId == 0x16 && valOff + 1 < data.Length && data[valOff] == 0xC4 && valOff + 2 + data[valOff + 1] <= data.Length)
                    {
                        int slen = data[valOff + 1];
                        if (slen >= 2 && slen <= 35)
                            name = Encoding.ASCII.GetString(data, valOff + 2, slen);
                    }
                    else if (propId == 0x1D && valOff < data.Length)
                    {
                        if (data[valOff] == 0x82)
                        {
                            teamId = ExtractObservedTeamFromMap(data, valOff, entityEnd);
                        }
                        else
                        {
                            int? t = DecodeMsgPackIntOrNil(data, valOff);
                            if (t.HasValue && t.Value >= 0 && t.Value <= 2)
                                teamId = t;
                        }
                    }
                    else if (propId == 0x1B && valOff + 5 <= data.Length && data[valOff] == 0xCE)
                    {
                        uint sid = (uint)(data[valOff + 1] << 24 | data[valOff + 2] << 16 | data[valOff + 3] << 8 | data[valOff + 4]);
                        if (sid > 1000000)
                            shipId = sid;
                    }
                    else if (propId == 0x25 && valOff + 5 <= data.Length && data[valOff] == 0xCE)
                    {
                        uint sid = (uint)(data[valOff + 1] << 24 | data[valOff + 2] << 16 | data[valOff + 3] << 8 | data[valOff + 4]);
                        if (sid > 1000000)
                            shipId = sid;
                    }
                }

                if (name != null && shipId.HasValue)
                {
                    entities.Add((name, name, shipId.Value, teamId ?? -1));
                }
            }
        }

        private static int? TryExtractObservedTeam(byte[] data)
        {
            byte[] key = Encoding.ASCII.GetBytes("observedTeamId");

            for (int i = 0; i <= data.Length - key.Length - 2; i++)
            {
                if (data[i] != 0xC4 && data[i] != 0xD9)
                    continue;

                int keyLen = 0, headerLen = 0;
                if (data[i] == 0xC4) { keyLen = data[i + 1]; headerLen = 2; }
                else if (data[i] == 0xD9) { keyLen = data[i + 1]; headerLen = 2; }
                if (keyLen != key.Length) continue;

                if (i + headerLen + keyLen > data.Length) continue;
                bool match = true;
                for (int j = 0; j < key.Length; j++)
                    if (data[i + headerLen + j] != key[j]) { match = false; break; }

                if (!match) continue;

                int valOff = i + headerLen + keyLen;
                if (valOff >= data.Length) continue;

                int? teamId = DecodeMsgPackIntOrNil(data, valOff);
                if (teamId.HasValue && teamId.Value >= 0 && teamId.Value <= 2)
                {
                    LogUtils.WriteDebug($"TryExtractObservedTeam: {teamId.Value} at offset 0x{i:X}");
                    return teamId;
                }
            }

            return null;
        }

        private static int? ExtractObservedTeamFromMap(byte[] data, int mapStart, int regionEnd)
        {
            byte[] key = Encoding.ASCII.GetBytes("observedTeamId");
            int pos = mapStart + 1;

            while (pos < Math.Min(regionEnd, mapStart + 200) && pos + key.Length <= data.Length)
            {
                if (data[pos] == 0xC4 || data[pos] == 0xD9)
                {
                    int headerLen = 1;
                    int keyLen = data[pos] == 0xC4 ? data[pos + 1] : data[pos + 1];
                    headerLen = 2;

                    if (keyLen == key.Length && pos + headerLen + keyLen <= data.Length)
                    {
                        bool match = true;
                        for (int j = 0; j < key.Length; j++)
                            if (data[pos + headerLen + j] != key[j]) { match = false; break; }

                        if (match)
                        {
                            int valOff = pos + headerLen + keyLen;
                            if (valOff < data.Length)
                            {
                                int? t = DecodeMsgPackIntOrNil(data, valOff);
                                if (t.HasValue && t.Value >= 0 && t.Value <= 2)
                                    return t;
                            }
                        }
                    }
                }
                pos++;
            }

            return null;
        }

        private static int? DecodeMsgPackIntOrNil(byte[] data, int offset)
        {
            if (offset >= data.Length)
                return null;

            byte b = data[offset];
            if (b == 0xC0 || b == 0xC2 || b == 0xC3)
                return null;
            if (b <= 0x7F)
                return b;
            if (b >= 0xE0)
                return b - 256;
            if (b == 0xCC && offset + 1 < data.Length)
                return data[offset + 1];
            if (b == 0xCD && offset + 2 < data.Length)
                return data[offset + 1] << 8 | data[offset + 2];
            return null;
        }

        private static ReadOnlySpan<byte> ReadBlock(byte[] data, ref int offset)
        {
            if (data.Length < offset + 4)
                throw new FileFormatException("BlockHeaderTruncated");

            uint length = BitConverter.ToUInt32(data, offset);
            offset += 4;

            if (data.Length < offset + length)
                throw new FileFormatException("BlockDataTruncated");

            ReadOnlySpan<byte> block = data.AsSpan(offset, (int)length);
            offset += (int)length;
            return block;
        }

        private static byte[]? TryReadReplayFile(string filePath)
        {
            long fileLen;
            using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                fileLen = fs.Length;
            }

            if (fileLen < 12)
                return null;

            byte[] data = new byte[fileLen];
            using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                int totalRead = 0;
                while (totalRead < data.Length)
                {
                    int read = fs.Read(data, totalRead, data.Length - totalRead);
                    if (read == 0)
                        break;
                    totalRead += read;
                }
            }

            int headerPos = IndexOf(data, ReplayHeader, 0);
            if (headerPos < 0)
                return null;

            int effectiveLen = data.Length - headerPos;
            if (effectiveLen < 8)
                return null;

            uint blockCount = BitConverter.ToUInt32(data, headerPos + 4);
            if (blockCount < 1 || blockCount > 10)
                return null;

            int blockEndOffset = headerPos + 8;
            for (uint bi = 0; bi < blockCount; bi++)
            {
                if (data.Length < blockEndOffset + 4)
                    return null;
                uint blockLen = BitConverter.ToUInt32(data, blockEndOffset);
                blockEndOffset += 4;
                if (data.Length < blockEndOffset + blockLen)
                    return null;
                blockEndOffset += (int)blockLen;
            }

            LogUtils.WriteInfo($"KorabliReplay [replay header] found at offset {headerPos}, {blockCount} blocks valid");

            byte[] replayData = new byte[effectiveLen];
            Array.Copy(data, headerPos, replayData, 0, effectiveLen);
            return replayData;
        }

        private static int IndexOf(byte[] data, byte[] pattern, int startIndex)
        {
            for (int i = startIndex; i <= data.Length - pattern.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }
            return -1;
        }

        private static byte[]? TryDecompress(byte[] data, int offset)
        {
            try
            {
                using var ms = new MemoryStream(data, offset, data.Length - offset);
                using var zs = new ZLibStream(ms, CompressionMode.Decompress);
                using var rs = new MemoryStream();
                zs.CopyTo(rs);
                return rs.ToArray();
            }
            catch { }
            try
            {
                using var ms = new MemoryStream(data, offset, data.Length - offset);
                using var ds = new DeflateStream(ms, CompressionMode.Decompress);
                using var rs = new MemoryStream();
                ds.CopyTo(rs);
                return rs.ToArray();
            }
            catch { }
            return null;
        }
    }
}
