using UnityEngine;
using System;
using System.Collections.Generic;

namespace Multi
{
    namespace BGA
    {
        /// This class has the following purposes:
        /// - provide access to notifSender (send notifications to BGA)
        /// - keep up to date the history of notifications, order them, and provide the next notification to process
        public class PacketHandler
        {
            private DateTime _lastChange;
            public enum RefreshLevel { NONE, SLOW, FAST }
            private RefreshLevel _refreshLevel;

            private List<PacketData> packets;
            private Dictionary<int, int> packetmap;
            private Dictionary<int, List<int>> movemap;
            private int currentMove;
            private int lastPacketId;
            private bool forceUpdate;

            public NotificationSender notifSender;

            public PacketHandler()
            {
                notifSender = new NotificationSender();
            }

            public void Clear()
            {
                packets = new List<PacketData>();
                packetmap = new Dictionary<int, int>();
                movemap = new Dictionary<int, List<int>>();
                currentMove = 0;
                lastPacketId = 0;
                notifSender.Clear();
                forceUpdate = false;
            }


            public void Reset(int initMoveid, int initPacketid)
            {
                Clear();
                currentMove = initMoveid;
                lastPacketId = initPacketid;
            }

            public void Start()
            {
                _refreshLevel = RefreshLevel.FAST;
                _lastChange = DateTime.Now;

                notifSender.Start();
            }

            public void Update(string tableId)
            {
                notifSender.Update();

                if (IsReadyToRefresh())
                {
                    Logger.Instance.Log("LOG", "ask for history " + tableId + ", p:" + (lastPacketId + 1));
                    if (Http.Instance.GetNotificationHistory(tableId, lastPacketId + 1, false, UpdateHistory))
                    {
                        Logger.Instance.Log("DETAIL", "ask success");
                        _lastChange = DateTime.Now;
                        forceUpdate = false;
                    }
                }
            }

            public void ForceUpdate()
            {
                forceUpdate = true;
            }

            public NotificationData ProcessCurrentNotification()
            {
                KeyValuePair<PacketData, NotificationData> notif = _GetCurrentNotification();
                notif.Value.MarkAsProcessing();
                notif.Key.MarkAsChanged();
                Logger.Instance.Log("LOG", "Process (move " + currentMove + ") Notification " + notif.Value.typeAsString);
                return notif.Value;
            }

            public void ResolveCurrentNotification()
            {
                KeyValuePair<PacketData, NotificationData> notif = _GetCurrentNotification();
                notif.Value.MarkAsResolved();
                notif.Key.MarkAsChanged();
                Logger.Instance.Log("LOG", "Resolve (move " + currentMove + ") Notification " + notif.Value.typeAsString);
                UpdateMove();
            }

            public void UpdateMove()
            {
                if (!HasUnresolvedPackets(currentMove))
                {
                    if (movemap.ContainsKey(currentMove + 1))
                    {
                        currentMove = currentMove + 1;
                    }
                }
                List<PacketData> unresolvedPackets = GetUnresolvedPackets(currentMove);
                foreach (PacketData packet in unresolvedPackets)
                {
                    if (packet.status == PacketData.Status.SLEEPING)
                        packet.MarkAsWaiting();
                }
            }

            public NotificationData GetCurrentNotification()
            {
                return _GetCurrentNotification().Value;
            }

            /*  parse all newly received packets.
             *
             *
             */
            public void UpdateHistory(JSONObject history)
            {
                if (history == null) return;

                List<PacketData> newPackets = ParseData(history);
                for (int i = 0; i < newPackets.Count; ++i)
                {
                    PacketData packet = newPackets[i];
                    if (packet.isValid)
                    {
                        if (packetmap.ContainsKey(packet.packetId))
                        {
                            Logger.Instance.Log("DETAIL", "packet " + packet.packetId + " already referenced");
                        }
                        else if (packet.packetId <= lastPacketId)
                        {
                            Logger.Instance.Log("DETAIL", "skip simplenote packet " + packet.packetId);
                        }
                        else
                        {
                            lastPacketId = packet.packetId;
                            packets.Add(packet);
                            packetmap[packet.packetId] = packets.Count - 1;
                            if (packet.packetType == PacketData.Type.HISTORY)
                            {
                                if (!movemap.ContainsKey(packet.moveId))
                                {
                                    movemap[packet.moveId] = new List<int>();
                                }
                                movemap[packet.moveId].Add(packets.Count - 1);
                            }
                            Logger.Instance.Log("LOG", "new packet :\n" + packet.debugPrintString);
                        }
                    }
                }
                UpdateMove();
            }

            public bool IsProcessingCurrentMove()
            {
                return IsMoveProcessing(currentMove);
            }

            // implementation details
            private bool IsMoveProcessing(int move)
            {
                if (movemap.ContainsKey(move))
                {
                    bool hasResolvedPacket = false;
                    foreach (int packetindex in movemap[move])
                    {
                        PacketData packet = packets[packetindex];
                        if (packet.isResolved())
                            hasResolvedPacket = true;
                        else if (hasResolvedPacket || packet.isProcessing())
                            return true;
                    }
                }
                return false;
            }

            private bool HasUnresolvedPackets(int move)
            {
                return GetUnresolvedPackets(move).Count > 0;
            }

            private List<PacketData> GetUnresolvedPackets(int move)
            {
                List<PacketData> unresolvedPackets = new List<PacketData>();
                if (movemap.ContainsKey(move))
                {
                    foreach (int packetindex in movemap[move])
                    {
                        PacketData packet = packets[packetindex];
                        Debug.Assert(packet.status != PacketData.Status.UNKNOWN, "ill-formd packet");
                        if (packet.status != PacketData.Status.RESOLVED && packet.status != PacketData.Status.UNKNOWN)
                        {
                            unresolvedPackets.Add(packet);
                        }
                    }
                }
                return unresolvedPackets;
            }

            private KeyValuePair<PacketData, NotificationData> _GetCurrentNotification()
            {
                List<PacketData> unresolvedPackets = GetUnresolvedPackets(currentMove);
                foreach (PacketData packet in unresolvedPackets)
                {
                    switch (packet.status)
                    {
                        case PacketData.Status.RESOLVED:
                            continue;
                        case PacketData.Status.UNKNOWN:
                            Logger.Instance.Log("ERROR", "ill-formated packet");
                            continue;
                        default:
                            return new KeyValuePair<PacketData, NotificationData>(packet, packet.GetCurrentNotification());
                    }
                }

                return new KeyValuePair<PacketData, NotificationData>(null, null);
            }

            private List<PacketData> ParseData(JSONObject json)
            {
                List<PacketData> newPackets = new List<PacketData>();

                if (json.HasField("status"))
                {
                    if (json.GetField("status").type == JSONObject.Type.NUMBER && json.GetField("status").n == 1 && json.HasField("data"))
                    {
                        json = json.GetField("data");
                    }
                    else
                    {
                        Logger.Instance.Log("WARNING", "GameData Parse Errror");
                        return newPackets;
                    }
                }

                if (json.HasField("valid"))
                {
                    if (json.GetField("valid").type == JSONObject.Type.NUMBER && json.GetField("valid").n == 1 && json.HasField("data"))
                    {
                        json = json.GetField("data");
                    }
                    else
                    {
                        Logger.Instance.Log("WARNING", "GameData Parse Errror");
                        return newPackets;
                    }
                }

                if (json.type == JSONObject.Type.ARRAY)
                {
                    for (int i = 0; i < json.Count; ++i)
                    {
                        newPackets.Add(new PacketData(json[i]));
                    }
                }
                else
                {
                    newPackets.Add(new PacketData(json));
                }

                return newPackets;
            }

            private bool IsReadyToRefresh()
            {
                if (forceUpdate)
                {
                    return true;
                }

                if (notifSender.isProcessing)
                {
                    return false;
                }

                switch (_refreshLevel)
                {
                    case RefreshLevel.FAST:
                        return DateTime.Now >= _lastChange + new TimeSpan(0, 0, 0, 1); // 1 seconds
                    case RefreshLevel.SLOW:
                        return DateTime.Now >= _lastChange + new TimeSpan(0, 0, 0, 3); // 3 seconds
                    case RefreshLevel.NONE:
                    default:
                        return false;
                }
            }
        }
    }
}
