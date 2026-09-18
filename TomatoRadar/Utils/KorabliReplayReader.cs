using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
            if (result == null)
            {
                try
                {
                    LogUtils.WriteInfo($"KorabliReplay: BOTH parsers failed for {filePath} (size={new FileInfo(filePath).Length} bytes). " +
                                       "If this is a live battle file, the recorded format may have changed.");
                }
                catch { }
            }
            return result;
        }

        /// <summary>
        /// Walks the [12 32 34 11] header and its length-prefixed blocks.
        /// A live battle file is still being written, so trailing truncation is tolerated:
        /// CompleteBlocks reports how many blocks are fully present.
        /// </summary>
        private sealed class ReplayHeaderInfo
        {
            public byte[] Data = Array.Empty<byte>();
            public uint BlockCount;
            public int CompleteBlocks;
        }

        private static JObject? TryReadReplayBlocksJSON(string filePath)
        {
            ReplayHeaderInfo? header = TryReadReplayFile(filePath);
            if (header == null)
                return null;

            byte[] data = header.Data;

            int offset = 8;
            ReadOnlySpan<byte> metaBlock = ReadBlock(data, ref offset);
            if (metaBlock.Length == 0)
                return null;

            string metaJson = Encoding.UTF8.GetString(metaBlock);
            JObject metaObj = JObject.Parse(metaJson);

            string matchGroup = metaObj["matchGroup"]?.Value<string>() ?? string.Empty;
            string dateTime = metaObj["dateTime"]?.Value<string>() ?? string.Empty;
            string playerName = metaObj["playerName"]?.Value<string>() ?? string.Empty;
            int isFogOfWar = metaObj["isFogOfWar"]?.Value<int>() ?? 0;

            LogUtils.WriteInfo($"KorabliReplay [replay header]: meta={matchGroup}, player={playerName}, isFogOfWar={isFogOfWar}, blocks={header.CompleteBlocks}/{header.BlockCount} complete");

            JArray vehiclesArray = new();

            if (header.BlockCount > 1 && header.CompleteBlocks > 1)
            {
                try
                {
                    ReadOnlySpan<byte> block1Data = ReadBlock(data, ref offset);
                    string block1Json = Encoding.UTF8.GetString(block1Data);

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

                        LogUtils.WriteInfo($"KorabliReplay [playersPublicInfo]: playerName={playerName}, currentTeam={currentTeam}, {players.Count} players");

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
            else if (header.BlockCount > 1)
            {
                LogUtils.WriteInfo($"KorabliReplay [replay header]: block 1 not written yet ({header.CompleteBlocks}/{header.BlockCount} complete), falling back to arena-info parser");
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
                DumpFailureArtifacts(filePath, data, decompBlocks);
                return null;
            }

            try
            {
                LogUtils.WriteInfo($"KorabliReplay [arena info]: {decompBlocks.Count} zlib blocks, sizes={string.Join(",", decompBlocks.Select(b => b.Length))}, file={data.Length} bytes");
            }
            catch { }

            // Preferred path: decode the payload as real MessagePack and read the documented
            // field ids. The old heuristic scanner keyed off hard-coded property ids that Lesta
            // shifted, which silently produced zero players on live battle files.
            JObject? messagePackResult = TryBuildVehiclesFromMessagePack(decompBlocks, filePath);
            if (messagePackResult != null)
            {
                LogUtils.WriteInfo("KorabliReplay [arena info]: roster decoded from MessagePack payload");
                return messagePackResult;
            }

            LogUtils.WriteInfo("KorabliReplay [arena info]: MessagePack decode produced no roster, falling back to heuristic scanner");

            var entities = new List<(string name, string displayName, ulong shipId, int teamId)>();

            foreach (var blk in decompBlocks)
            {
                ParseMessagePackEntities(blk, entities);
            }

            if (entities.Count == 0)
            {
                foreach (var blk in decompBlocks)
                {
                    if (blk.Length > 2 && blk[0] == 0x7B)
                    {
                        try
                        {
                            string jsonStr = Encoding.UTF8.GetString(blk);
                            JObject jsonObj = JObject.Parse(jsonStr);
                            JArray? vehicles = jsonObj["vehicles"] as JArray;
                            if (vehicles != null)
                            {
                                LogUtils.WriteInfo("KorabliReplay [arena info]: found JSON vehicles array in decompressed block");
                                foreach (var v in vehicles)
                                {
                                    string? vName = v["name"]?.Value<string>();
                                    string? vShipId = v["shipId"]?.Value<string>();
                                    int vRelation = v["relation"]?.Value<int>() ?? 1;
                                    if (vName != null && vShipId != null && vRelation > 0)
                                    {
                                        entities.Add((vName, vName, ulong.Parse(vShipId), vRelation));
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
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
                DumpFailureArtifacts(filePath, data, decompBlocks);
                return null;
            }

            if (entities.Count > 0 && entities.All(e => e.teamId < 0))
            {
                var playerTeams = new List<int>();
                foreach (var blk in decompBlocks)
                {
                    ExtractPlayerObservedTeams(blk, playerTeams);
                }
                LogUtils.WriteInfo($"KorabliReplay [arena info]: extracted {playerTeams.Count} player observedTeams for {entities.Count} entities");
                for (int i = 0; i < entities.Count && i < playerTeams.Count; i++)
                {
                    entities[i] = (entities[i].name, entities[i].displayName, entities[i].shipId, playerTeams[i]);
                }
            }

            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (dir != null)
                {
                    string arenaJsonPath = Path.Combine(dir, "tempArenaInfo.json");
                    if (File.Exists(arenaJsonPath))
                    {
                        JObject arenaJson = FileUtils.ReadTempArenaInfoFile(arenaJsonPath);
                        currentPlayerName = arenaJson["playerName"]?.Value<string>();
                    }
                }
            }
            catch { }

            int currentTeam;
            if (observedTeam.HasValue)
            {
                currentTeam = observedTeam.Value;
            }
            else
            {
                int? matchedTeamId = null;
                if (currentPlayerName != null)
                {
                    foreach (var e in entities)
                    {
                        if (string.Equals(e.displayName, currentPlayerName, StringComparison.OrdinalIgnoreCase) && e.teamId >= 0)
                        {
                            matchedTeamId = e.teamId;
                            break;
                        }
                    }
                }

                if (matchedTeamId.HasValue)
                {
                    currentTeam = matchedTeamId.Value;
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
            }

            var vehiclesArray = new JArray();
            int idCounter = 100;

            LogUtils.WriteInfo($"KorabliReplay [arena info]: playerName={currentPlayerName}, observedTeam={observedTeam}, currentTeam={currentTeam}");

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

            for (int i = 0; i <= data.Length - 4; i++)
            {
                if (data[i] != 0x92 || data[i + 1] != 0x00)
                    continue;

                bool isBoundary = false;

                if (i + 7 <= data.Length && data[i + 2] == 0xCE)
                {
                    uint acctId = (uint)(data[i + 3] << 24 | data[i + 4] << 16 | data[i + 5] << 8 | data[i + 6]);
                    if (acctId > 100)
                        isBoundary = true;
                }
                else if (i + 7 <= data.Length && data[i + 2] == 0xD2)
                {
                    int acctId = data[i + 3] << 24 | data[i + 4] << 16 | data[i + 5] << 8 | data[i + 6];
                    if (acctId < 0)
                        isBoundary = true;
                }
                else if (i + 5 <= data.Length && data[i + 2] == 0xCD)
                {
                    ushort acctId = (ushort)(data[i + 3] << 8 | data[i + 4]);
                    if (acctId > 100)
                        isBoundary = true;
                }
                else if (i + 4 <= data.Length && data[i + 2] == 0xCC)
                {
                    byte acctId = data[i + 3];
                    if (acctId > 100)
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

                    if (propId == 0x1C && valOff + 1 < data.Length)
                    {
                        if (data[valOff] == 0xC4 && valOff + 2 + data[valOff + 1] <= data.Length)
                        {
                            int slen = data[valOff + 1];
                            if (slen >= 2 && slen <= 64)
                                name = Encoding.UTF8.GetString(data, valOff + 2, slen);
                        }
                        else if (data[valOff] == 0xD9 && valOff + 3 + (data[valOff + 1] << 8 | data[valOff + 2]) <= data.Length)
                        {
                            int slen = data[valOff + 1] << 8 | data[valOff + 2];
                            if (slen >= 2 && slen <= 64)
                                name = Encoding.UTF8.GetString(data, valOff + 3, slen);
                        }
                    }
                    else if (propId == 0x16 && valOff + 1 < data.Length)
                    {
                        if (data[valOff] == 0xC4 && valOff + 2 + data[valOff + 1] <= data.Length)
                        {
                            int slen = data[valOff + 1];
                            if (slen >= 2 && slen <= 64)
                                name = Encoding.UTF8.GetString(data, valOff + 2, slen);
                        }
                        else if (data[valOff] == 0xD9 && valOff + 3 + (data[valOff + 1] << 8 | data[valOff + 2]) <= data.Length)
                        {
                            int slen = data[valOff + 1] << 8 | data[valOff + 2];
                            if (slen >= 2 && slen <= 64)
                                name = Encoding.UTF8.GetString(data, valOff + 3, slen);
                        }
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

        // ==================================================================
        // MessagePack decoding of the Lesta live arena-info payload.
        //
        // Verified field layout of one player record (array of [fieldId, value] pairs):
        //   0  -> accountDBID
        //   8  -> clan tag
        //   29 -> player nickname
        //   30 -> map { playerModeType, observedTeamId }
        //   34 -> realm ("RU")
        //   38 -> ship id (matches Resources/Json/ships_lesta.json)
        // Records with a negative synthetic id are the "unidentified enemy" entries; they
        // carry a ship id where the nickname would be and cannot be looked up, so they are
        // skipped.
        // ==================================================================

        private sealed class MsgPackReader
        {
            private readonly byte[] _d;
            private int _p;

            public MsgPackReader(byte[] data) { _d = data; }

            private void Need(int n)
            {
                if (_p + n > _d.Length)
                    throw new InvalidDataException($"msgpack truncated at {_p} (need {n})");
            }

            private ushort U16() { Need(2); ushort v = (ushort)((_d[_p] << 8) | _d[_p + 1]); _p += 2; return v; }
            private uint U32() { Need(4); uint v = (uint)((_d[_p] << 24) | (_d[_p + 1] << 16) | (_d[_p + 2] << 8) | _d[_p + 3]); _p += 4; return v; }

            private byte[] ReadBin(int n) { Need(n); byte[] v = new byte[n]; Array.Copy(_d, _p, v, 0, n); _p += n; return v; }
            private string ReadString(int n) { Need(n); string v = Encoding.UTF8.GetString(_d, _p, n); _p += n; return v; }

            private object?[] ReadArray(int n)
            {
                object?[] a = new object?[n];
                for (int i = 0; i < n; i++) a[i] = Read();
                return a;
            }

            private Dictionary<string, object?> ReadMap(int n)
            {
                Dictionary<string, object?> m = new();
                for (int i = 0; i < n; i++)
                {
                    object? k = Read();
                    object? v = Read();
                    m[Convert.ToString(k, System.Globalization.CultureInfo.InvariantCulture) ?? ""] = v;
                }
                return m;
            }

            public object? Read()
            {
                Need(1);
                byte b = _d[_p++];

                if (b <= 0x7F) return (long)b;
                if (b >= 0xE0) return (long)(sbyte)b;
                if (b >= 0x80 && b <= 0x8F) return ReadMap(b & 0x0F);
                if (b >= 0x90 && b <= 0x9F) return ReadArray(b & 0x0F);
                if (b >= 0xA0 && b <= 0xBF) return ReadString(b & 0x1F);

                switch (b)
                {
                    case 0xC0: return null;
                    case 0xC2: return false;
                    case 0xC3: return true;
                    case 0xC4: return ReadBin(_d[_p++]);
                    case 0xC5: return ReadBin(U16());
                    case 0xC6: return ReadBin((int)U32());
                    case 0xC7: { int n = _d[_p++]; _p++; return ReadBin(n); }
                    case 0xC8: { int n = U16(); _p++; return ReadBin(n); }
                    case 0xC9: { int n = (int)U32(); _p++; return ReadBin(n); }
                    case 0xCA: { Need(4); float v = BitConverter.ToSingle(_d, _p); _p += 4; return (double)v; }
                    case 0xCB: { Need(8); double v = BitConverter.ToDouble(_d, _p); _p += 8; return v; }
                    case 0xCC: return (long)_d[_p++];
                    case 0xCD: return (long)U16();
                    case 0xCE: return (long)U32();
                    case 0xCF: { Need(8); ulong v = BitConverter.ToUInt64(_d, _p); _p += 8; return (long)v; }
                    case 0xD0: return (long)(sbyte)_d[_p++];
                    case 0xD1: { Need(2); short v = BitConverter.ToInt16(_d, _p); _p += 2; return (long)v; }
                    case 0xD2: { Need(4); int v = BitConverter.ToInt32(_d, _p); _p += 4; return (long)v; }
                    case 0xD3: { Need(8); long v = BitConverter.ToInt64(_d, _p); _p += 8; return v; }
                    case 0xD9: return ReadString(_d[_p++]);
                    case 0xDA: return ReadString(U16());
                    case 0xDB: return ReadString((int)U32());
                    case 0xDC: return ReadArray(U16());
                    case 0xDD: return ReadArray((int)U32());
                    case 0xDE: return ReadMap(U16());
                    case 0xDF: return ReadMap((int)U32());
                    default:
                        if (b >= 0xD4 && b <= 0xD8) { int n = 1 << (b - 0xD4); _p++; return ReadBin(n); }
                        throw new InvalidDataException($"unhandled msgpack byte 0x{b:X2} at {_p - 1}");
                }
            }
        }

        private static bool IsAllDigits(string s)
        {
            if (s.Length == 0) return false;
            foreach (char c in s)
                if (c < '0' || c > '9') return false;
            return true;
        }

        private static List<(string name, long acctId, long shipId, int teamId)>? DecodeArenaBlock(
            byte[] block,
            List<(long shipId, int teamId)> unidentified)
        {
            MsgPackReader reader = new(block);
            object? root = reader.Read();
            if (root is not object[] records)
                return null;

            List<(string, long, long, int)> result = new();

            foreach (object? recObj in records)
            {
                if (recObj is not object[] rec)
                    continue;

                long acctId = -1;
                long shipId = -1;
                long nameAsShipId = 0;
                int teamId = -1;
                string? name = null;

                foreach (object? pairObj in rec)
                {
                    if (pairObj is not object[] pair || pair.Length < 2) continue;
                    if (pair[0] is not long key) continue;
                    object? val = pair[1];

                    switch (key)
                    {
                        case 0:
                            if (val is long accountValue) acctId = accountValue;
                            break;
                        case 29:
                            if (val is string nick)
                            {
                                name = nick;
                                // Unidentified enemies carry a ship id where the nickname would be.
                                if (IsAllDigits(nick) && long.TryParse(nick, out long parsedShipId))
                                    nameAsShipId = parsedShipId;
                            }
                            break;
                        case 38:
                            if (val is long shipValue) shipId = shipValue;
                            break;
                        case 40:
                            if (val is long relationValue) teamId = (int)relationValue;
                            break;
                        case 30:
                            if (val is Dictionary<string, object?> extras &&
                                extras.TryGetValue("observedTeamId", out object? observed) &&
                                observed is long observedTeam)
                                teamId = (int)observedTeam;
                            break;
                    }
                }

                if (acctId > 0 && shipId > 0 && !string.IsNullOrEmpty(name) && !IsAllDigits(name))
                    result.Add((name!, acctId, shipId, teamId));
                else if (acctId <= 0 && nameAsShipId > 0)
                    unidentified.Add((nameAsShipId, teamId));
            }

            return result.Count > 0 ? result : null;
        }

        private static string? TryReadSelfPlayerName(string filePath)
        {
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (dir == null) return null;
                string jsonPath = Path.Combine(dir, "tempArenaInfo.json");
                if (!File.Exists(jsonPath)) return null;
                JObject arenaJson = FileUtils.ReadTempArenaInfoFile(jsonPath);
                return arenaJson["playerName"]?.Value<string>();
            }
            catch
            {
                return null;
            }
        }

        private static JObject? TryBuildVehiclesFromMessagePack(List<byte[]> decompBlocks, string filePath)
        {
            List<(string name, long acctId, long shipId, int teamId)> players = new();
            List<(long shipId, int teamId)> unidentified = new();
            HashSet<long> seenAccounts = new();

            foreach (byte[] block in decompBlocks)
            {
                List<(string name, long acctId, long shipId, int teamId)>? part;
                try
                {
                    part = DecodeArenaBlock(block, unidentified);
                }
                catch (Exception ex)
                {
                    LogUtils.WriteDebug($"arena msgpack decode failed: {ex.Message}");
                    continue;
                }

                if (part == null) continue;

                foreach (var p in part)
                {
                    if (seenAccounts.Add(p.acctId))
                        players.Add(p);
                }
            }

            if (players.Count == 0)
                return null;

            string? selfName = TryReadSelfPlayerName(filePath);
            int currentTeam = -1;
            if (!string.IsNullOrEmpty(selfName))
            {
                foreach (var p in players)
                {
                    if (string.Equals(p.name, selfName, StringComparison.OrdinalIgnoreCase))
                    {
                        currentTeam = p.teamId;
                        break;
                    }
                }
            }
            if (currentTeam < 0) currentTeam = players[0].teamId;
            if (currentTeam < 0) currentTeam = 1;

            JArray vehiclesArray = new();
            int idCounter = 100;
            foreach (var p in players)
            {
                int relation = (p.teamId == currentTeam) ? 1 : 8;
                vehiclesArray.Add(new JObject
                {
                    ["name"] = p.name,
                    ["shipId"] = p.shipId.ToString(),
                    ["relation"] = relation,
                    ["id"] = idCounter++,
                });
            }

            string matchGroup = "battle";
            string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (dir != null)
                {
                    string jsonPath = Path.Combine(dir, "tempArenaInfo.json");
                    if (File.Exists(jsonPath))
                    {
                        JObject arenaJson = FileUtils.ReadTempArenaInfoFile(jsonPath);
                        matchGroup = arenaJson["matchGroup"]?.Value<string>() ?? matchGroup;
                        dateTime = arenaJson["dateTime"]?.Value<string>() ?? dateTime;
                    }
                }
            }
            catch { }

            bool rosterChanged = DumpRosterSnapshot(filePath, players, unidentified);
            if (rosterChanged)
                LogUtils.WriteInfo($"KorabliReplay [arena info]: msgpack roster = {players.Count} identified, {unidentified.Count} unidentified, self=\"{selfName}\", currentTeam={currentTeam}");

            return new JObject
            {
                ["vehicles"] = vehiclesArray,
                ["matchGroup"] = matchGroup,
                ["dateTime"] = dateTime,
            };
        }

        private static string _lastRosterSignature = "";

        /// <summary>
        /// Writes one text snapshot per distinct roster shape so the real composition of a
        /// live battle (identified players vs. unidentified enemy entries) can be reviewed
        /// offline without a second battle.
        /// </summary>
        private static bool DumpRosterSnapshot(
            string filePath,
            List<(string name, long acctId, long shipId, int teamId)> players,
            List<(long shipId, int teamId)> unidentified)
        {
            try
            {
                string signature = $"{players.Count}/{unidentified.Count}";
                if (signature == _lastRosterSignature)
                    return false;
                _lastRosterSignature = signature;

                string dir = Path.Combine(App.DataDirectory, "Log", "korabli_dump");
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("HHmmss");

                StringBuilder sb = new();
                sb.AppendLine($"file={filePath}");
                sb.AppendLine($"identified={players.Count}");
                sb.AppendLine($"unidentified={unidentified.Count}");
                sb.AppendLine("--- identified players ---");
                foreach (var p in players)
                    sb.AppendLine($"P name={p.name} acct={p.acctId} ship={p.shipId} team={p.teamId}");
                sb.AppendLine("--- unidentified entries (ship id only) ---");
                foreach (var g in unidentified)
                    sb.AppendLine($"U ship={g.shipId} team={g.teamId}");

                File.WriteAllText(Path.Combine(dir, $"roster_{stamp}_{players.Count}p_{unidentified.Count}u.txt"), sb.ToString());
                LogUtils.WriteInfo($"KorabliReplay [arena info]: roster snapshot saved ({players.Count} identified, {unidentified.Count} unidentified)");
                return true;
            }
            catch (Exception ex)
            {
                LogUtils.WriteDebug($"roster snapshot failed: {ex.Message}");
                return false;
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

        private static void ExtractPlayerObservedTeams(byte[] data, List<int> teams)
        {
            byte[] key = Encoding.UTF8.GetBytes("observedTeamId");
            for (int i = 0; i <= data.Length - key.Length - 3; i++)
            {
                if (data[i] != 0xC4 || data[i + 1] != key.Length)
                    continue;
                bool match = true;
                for (int j = 0; j < key.Length; j++)
                    if (data[i + 2 + j] != key[j]) { match = false; break; }
                if (!match) continue;
                int valOff = i + 2 + key.Length;
                if (valOff >= data.Length) continue;
                int? t = DecodeMsgPackIntOrNil(data, valOff);
                if (t.HasValue)
                    teams.Add(t.Value);
            }
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

        private static ReplayHeaderInfo? TryReadReplayFile(string filePath)
        {
            long fileLen;
            try
            {
                using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                fileLen = fs.Length;
            }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }

            if (fileLen < 12)
                return null;

            byte[] data = new byte[fileLen];
            int totalRead = 0;
            using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
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
            {
                LogUtils.WriteInfo($"KorabliReplay [replay header]: NOT FOUND in {totalRead} bytes (first bytes: {BitConverter.ToString(data, 0, Math.Min(totalRead, 24)).Replace("-", " ")})");
                return null;
            }

            int effectiveLen = totalRead - headerPos;
            if (effectiveLen < 8)
            {
                LogUtils.WriteInfo($"KorabliReplay [replay header]: found at {headerPos} but only {effectiveLen} bytes follow");
                return null;
            }

            uint blockCount = BitConverter.ToUInt32(data, headerPos + 4);
            if (blockCount < 1 || blockCount > 10)
            {
                LogUtils.WriteInfo($"KorabliReplay [replay header]: found at {headerPos} but blockCount={blockCount} is out of range");
                return null;
            }

            // Count how many blocks are fully present. A live battle file is still being written,
            // so the trailing blocks are expected to be truncated. Only the leading blocks (meta
            // and playersPublicInfo) are actually needed to build the roster.
            int blockEndOffset = headerPos + 8;
            int completeBlocks = 0;
            for (uint bi = 0; bi < blockCount; bi++)
            {
                if (totalRead < blockEndOffset + 4)
                    break;
                uint blockLen = BitConverter.ToUInt32(data, blockEndOffset);
                blockEndOffset += 4;
                if (totalRead < blockEndOffset + blockLen)
                    break;
                blockEndOffset += (int)blockLen;
                completeBlocks++;
            }

            if (completeBlocks < 1)
            {
                uint firstLen = 0;
                try { firstLen = BitConverter.ToUInt32(data, headerPos + 8); } catch { }
                LogUtils.WriteInfo($"KorabliReplay [replay header]: found at {headerPos}, blockCount={blockCount}, but block 0 is incomplete " +
                                   $"(declared len={firstLen}, file={totalRead} bytes) - file still being written");
                return null;
            }

            LogUtils.WriteInfo($"KorabliReplay [replay header] found at offset {headerPos}, {completeBlocks}/{blockCount} blocks complete, file={totalRead} bytes");

            byte[] replayData = new byte[effectiveLen];
            Array.Copy(data, headerPos, replayData, 0, effectiveLen);
            return new ReplayHeaderInfo
            {
                Data = replayData,
                BlockCount = blockCount,
                CompleteBlocks = completeBlocks,
            };
        }

        // ------------------------------------------------------------------
        // Diagnostics: when the arena-info parser cannot make sense of a live
        // battle file, save the decompressed blocks so the real layout can be
        // reverse engineered offline.
        // ------------------------------------------------------------------
        private static readonly HashSet<string> _dumpedSignatures = new();
        private static int _dumpCount;
        private const int MaxDumps = 8;

        private static void DumpFailureArtifacts(string filePath, byte[]? rawData, List<byte[]> decompBlocks)
        {
            try
            {
                long size = 0;
                try { size = new FileInfo(filePath).Length; } catch { }

                string signature = size + ":" + (decompBlocks.Count > 0 ? decompBlocks[0].Length : 0);
                if (_dumpedSignatures.Contains(signature))
                    return;
                if (_dumpCount >= MaxDumps)
                    return;
                _dumpedSignatures.Add(signature);
                _dumpCount++;

                string dir = Path.Combine(App.DataDirectory, "Log", "korabli_dump");
                Directory.CreateDirectory(dir);
                string stamp = DateTime.Now.ToString("HHmmss") + "_" + size;

                StringBuilder sb = new();
                sb.AppendLine($"file={filePath}");
                sb.AppendLine($"size={size}");
                sb.AppendLine($"zlibBlockCount={decompBlocks.Count}");
                sb.AppendLine($"zlibBlockSizes={string.Join(",", decompBlocks.Select(b => b.Length))}");

                if (rawData != null)
                {
                    int head = Math.Min(rawData.Length, 256);
                    sb.AppendLine($"rawHeadHex={BitConverter.ToString(rawData, 0, head).Replace("-", " ")}");
                    sb.AppendLine($"rawHeadAscii={ToAscii(rawData, 512)}");
                }

                for (int i = 0; i < decompBlocks.Count && i < 4; i++)
                {
                    byte[] b = decompBlocks[i];
                    sb.AppendLine($"--- block{i} length={b.Length}");
                    sb.AppendLine($"hex={BitConverter.ToString(b, 0, Math.Min(b.Length, 256)).Replace("-", " ")}");
                    sb.AppendLine($"ascii={ToAscii(b, 1024)}");
                    if (b.Length <= 4 * 1024 * 1024)
                        File.WriteAllBytes(Path.Combine(dir, $"block{i}_{b.Length}_{stamp}.bin"), b);
                }

                File.WriteAllText(Path.Combine(dir, $"summary_{stamp}.txt"), sb.ToString());
                LogUtils.WriteInfo($"KorabliReplay: dumped diagnostics to {dir}\\summary_{stamp}.txt");
            }
            catch (Exception ex)
            {
                LogUtils.WriteInfo($"KorabliReplay: diagnostic dump failed: {ex.Message}");
            }
        }

        private static string ToAscii(byte[] data, int max)
        {
            int n = Math.Min(data.Length, max);
            char[] chars = new char[n];
            for (int i = 0; i < n; i++)
            {
                byte b = data[i];
                chars[i] = (b >= 32 && b < 127) ? (char)b : '.';
            }
            return new string(chars);
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
