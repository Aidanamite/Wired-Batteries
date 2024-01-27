using HarmonyLib;
using HMLLibrary;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using I2.Loc;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


namespace WiredBatteries
{
    public class Main : Mod
    {
        Transform prefabHolder;
        Harmony harmony;
        public Item_Base stationItem;
        public Item_Base jointItem;
        public Item_Base batteryItem;
        public LanguageSourceData language;
        public static Main instance;
        public override bool CanUnload(ref string message)
        {
            if (ComponentManager<Raft_Network>.Value?.remoteUsers?.Count > 1)
            {
                message = "This cannot be unloaded while in a multiplayer";
                return false;
            }
            return base.CanUnload(ref message);
        }
        bool loaded = false;
        public void Start()
        {
            if (ComponentManager<Raft_Network>.Value?.remoteUsers?.Count > 1)
            {
                Debug.LogError($"[{name}]: This cannot be loaded while in a multiplayer");
                modlistEntry.modinfo.unloadBtn.GetComponent<Button>().onClick.Invoke();
                return;
            }
            loaded = true;
            instance = this;
            prefabHolder = new GameObject("prefabHolder").transform;
            prefabHolder.gameObject.SetActive(false);
            DontDestroyOnLoad(prefabHolder.gameObject);

            var ziplineItem = ItemManager.GetItemByIndex(308);
            var lanternItem = ItemManager.GetItemByIndex(434);
            var connectorPrefab = new GameObject("electricConnector");
            connectorPrefab.transform.SetParent(prefabHolder, false);
            var m = new GameObject("model");
            m.transform.SetParent(connectorPrefab.transform, false);
            var m2 = lanternItem.settings_buildable.GetBlockPrefab(0).transform.GetChild(0);
            m.AddComponent<MeshFilter>().sharedMesh = m2.GetComponent<MeshFilter>().sharedMesh;
            m.AddComponent<MeshRenderer>().material = m2.GetComponent<MeshRenderer>().material;
            m.transform.localPosition = new Vector3(0, -0.2f, 0);
            m.transform.localScale = new Vector3(1, 0.5f, 1);
            m = new GameObject("righter");
            m.transform.SetParent(connectorPrefab.transform, false);
            m.AddComponent<MeshPathBaseParent>();
            m2 = m.transform;
            m = new GameObject("pathBase");
            m.transform.SetParent(m2, false);
            var b = m.AddComponent<MeshPathBase_RaftElectricity>();
            var c = m.AddComponent<BoxCollider>();
            c.size = Vector3.one * 0.3f;
            c.center = Vector3.zero;
            c.isTrigger = true;
            c.enabled = false;
            m.layer = 10;
            m.AddComponent<RaycastInteractable>();
            b.CopyFieldsOf(ziplineItem.settings_buildable.GetBlockPrefab(0).GetComponentInChildren<MeshPathBase>());
            b.enabled = true;
            b.connectPoint = m.transform;
            b.termStartDraggingRope = "Game/Cable/StartDragging";
            b.termEndDraggingRope = "Game/Cable/StopDragging";
            var p = Instantiate(b.pathPrefab, prefabHolder, false);
            var np = p.gameObject.AddComponent<MeshPath_RaftElectricity>();
            np.CopyFieldsOf(p);
            DestroyImmediate(p);
            Traverse.Create(np).Field("colliderRopeWidthMultiplier").SetValue(1.5f);
            b.pathPrefab = np;
            var r = np.GetComponent<MeshRenderer>();
            r.sharedMaterial = Instantiate(r.sharedMaterial);
            r.sharedMaterial.name = "ElectricalCableRaft";
            r.sharedMaterial.color = new Color(0.2f, 0.2f, 0.2f);


            var chargerItem = ItemManager.GetItemByIndex(293);
            var prefabBase = chargerItem.settings_buildable.GetBlockPrefab(0).GetComponentInChildren<Battery>().gameObject;
            var batteryPrefab = Instantiate(prefabBase, prefabHolder, false);
            batteryPrefab.AddComponent<BatteryAccess>();
            batteryPrefab.AddComponent<NoCableBattery>();
            batteryPrefab.GetComponent<Battery>().On = false;
            batteryPrefab.transform.localScale = prefabBase.transform.lossyScale;

            var brickItem = ItemManager.GetItemByIndex(72);
            var batteryObjPrefab = Instantiate(batteryPrefab.GetComponent<Battery>().itemEnabler.GetObjectConnections().First(x => x.item.UniqueIndex == 176).model,prefabHolder);
            batteryObjPrefab.name = "BatterySlot";
            m2 = brickItem.settings_buildable.GetBlockPrefab(0).transform.Find("model");
            batteryObjPrefab.GetComponent<MeshRenderer>().sharedMaterial = m2.GetComponent<MeshRenderer>().sharedMaterial;
            batteryObjPrefab.GetComponent<MeshFilter>().sharedMesh = m2.GetComponent<MeshFilter>().sharedMesh;
            batteryObjPrefab.transform.localPosition += new Vector3(0,-0.1f,0);
            batteryObjPrefab.transform.localRotation *= Quaternion.Euler(0, 90, 0);
            batteryObjPrefab.transform.localScale *= 0.8f;
            batteryObjPrefab.SetActive(false);
            var connnector = Instantiate(connectorPrefab, batteryObjPrefab.transform, false);
            connnector.transform.localPosition = new Vector3(0, 0.3f, 0);
            connnector.GetComponentInChildren<MeshPathBase_RaftElectricity>().GetComponent<Collider>().enabled = true;

            language = new LanguageSourceData()
            { 
                mDictionary = new Dictionary<string, TermData>
                {
                    ["Item/CableBattery"] = new TermData() { Languages = new[] { "Cable Battery@A battery shaped connector. Good for connecting a battery powered machine to an electrical network." } },
                    ["Item/Placeable_CableConnector"] = new TermData() { Languages = new[] { "Cable Connector@A simple connection point for electrical cables." } },
                    ["Item/Placeable_BatteryStation"] = new TermData() { Languages = new[] { "Battery Station@A stand with several battery slots. Use this to connect batteries to an electrical network." } },
                    ["CraftingSub/Cabling"] = new TermData() { Languages = new[] { "Cabling" } }
                },
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);

            var plankItem = ItemManager.GetItemByIndex(21);
            batteryItem = plankItem.Clone(10544, "CableBattery");
            batteryItem.name = batteryItem.UniqueName;
            batteryItem.settings_Inventory.LocalizationTerm = "Item/CableBattery";
            batteryItem.settings_Inventory.Sprite = LoadImage("CableBattery.png").ToSprite();
            batteryItem.SetRecipe(new[]
                {
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(73) }, 1),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(177) }, 1),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(178) }, 2)
                }, CraftingCategory.Other, 1, false, "Cabling", 2);

            EditBattery = battery =>
            {
                var batCol = battery.GetComponent<BoxCollider>().size;
                var size = (batCol.x + batCol.y + batCol.z) / 3;
                var bat = battery.itemEnabler?.GetObjectConnections()?.FirstOrDefault(x => x.item.UniqueIndex == 176)?.model;
                if (bat != null)
                {
                    var model = Instantiate(batteryObjPrefab, bat.transform.parent);
                    var col = model.GetComponentInChildren<BoxCollider>();
                    if (col.size.x < size)
                        col.size = Vector3.one * size;
                    Traverse.Create(battery.itemEnabler).Field("itemConnections").SetValue(battery.itemEnabler.GetObjectConnections().AddToArray(new ItemModelConnection() { item = batteryItem, model = model }));
                }
            };


            jointItem = brickItem.Clone(10543, "Placeable_CableConnector");
            jointItem.name = jointItem.UniqueName;
            jointItem.settings_Inventory.LocalizationTerm = "Item/Placeable_CableConnector";
            jointItem.settings_Inventory.Sprite = LoadImage("CableConnector.png").ToSprite();
            jointItem.SetRecipe(new[]
                {
                    new CostMultiple(new[] { plankItem }, 2),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(177) }, 1)
                }, CraftingCategory.Other, 1, false, "Cabling", 1);

            var jointPrefab = Instantiate(jointItem.settings_buildable.GetBlockPrefab(0), prefabHolder, false);
            jointPrefab.name = "Block_CableConnector_Floor";
            var cc = jointPrefab.gameObject.AddComponent<Block_CableConnector>();
            cc.CopyFieldsOf(jointPrefab);
            cc.ReplaceValues(jointPrefab, cc);
            jointPrefab.ReplaceValues(brickItem, jointItem);
            DestroyImmediate(jointPrefab);
            jointPrefab = cc;
            DestroyImmediate(jointPrefab.GetComponent<Brick_Wet>());
            DestroyImmediate(jointPrefab.GetComponent<RaycastInteractable>());
            DestroyImmediate(jointPrefab.transform.Find("model").gameObject);
            m = new GameObject("model");
            m.transform.SetParent(jointPrefab.transform, false);
            var pillarItem = ItemManager.GetItemByIndex(400);
            m2 = pillarItem.settings_buildable.GetBlockPrefab(0).transform.GetChild(0);
            m.AddComponent<MeshFilter>().sharedMesh = m2.GetComponent<MeshFilter>().sharedMesh;
            m.AddComponent<MeshRenderer>().material = m2.GetComponent<MeshRenderer>().material;
            m.transform.localScale = new Vector3(1.4f, 0.73f, 1.4f);
            c = jointPrefab.GetComponent<BoxCollider>();
            c.size = new Vector3(0.25f, 0.73f, 0.25f);
            c.center = new Vector3(0, c.size.y / 2, 0);
            var prefabs = new[] { jointPrefab, null, null };

            jointPrefab = Instantiate(jointPrefab, prefabHolder, false);
            jointPrefab.name = "Block_CableConnector_Ceiling";
            jointPrefab.dpsType = DPS.Ceiling;
            DestroyImmediate(jointPrefab.transform.Find("model").gameObject);
            m = new GameObject("model");
            m.transform.SetParent(jointPrefab.transform, false);
            m2 = ziplineItem.settings_buildable.GetBlockPrefab(2).transform.GetChild(2);
            m.AddComponent<MeshFilter>().sharedMesh = m2.GetComponent<MeshFilter>().sharedMesh;
            m.AddComponent<MeshRenderer>().material = m2.GetComponent<MeshRenderer>().material;
            m.transform.localRotation = Quaternion.Euler(-90, 0, 0);
            m.transform.localPosition = new Vector3(0, 0.07f, 0);
            c = jointPrefab.GetComponent<BoxCollider>();
            c.size = new Vector3(0.3f, 0.1f, 0.3f);
            c.center = new Vector3(0, c.size.y / 2 * 1.5f, 0);
            prefabs[1] = jointPrefab;
            jointPrefab = Instantiate(jointPrefab, prefabHolder, false);
            jointPrefab.name = "Block_CableConnector_Wall";
            jointPrefab.perpendicularAxis = Axis.Z;
            jointPrefab.dpsType = DPS.Wall;
            c = jointPrefab.GetComponent<BoxCollider>();
            c.center = new Vector3(c.center.x, -c.center.z, c.center.y);
            c.size = new Vector3(c.size.x, c.size.z, c.size.y);
            m2 = jointPrefab.transform.Find("model");
            m2.localRotation = Quaternion.Euler(0, 0, 90);
            m2.localPosition = new Vector3(0, 0, 0f);
            m2 = jointPrefab.transform.Find("gizmoCollider_required");
            m2.localRotation *= Quaternion.Euler(90, 0, 0);
            m2.localPosition = Quaternion.Euler(90, 0, 0) * m2.localPosition;
            prefabs[2] = jointPrefab;
            Traverse.Create(jointItem.settings_buildable).Field("blockPrefabs").SetValue(prefabs);

            Instantiate(connectorPrefab, prefabs[0].transform, false).transform.localPosition = new Vector3(0, 1f, 0);
            Instantiate(connectorPrefab, prefabs[1].transform, false).transform.localPosition = new Vector3(0, 0.22f, 0);
            m2 = Instantiate(connectorPrefab, prefabs[2].transform, false).transform;
            m2.localPosition = new Vector3(0, 0, 0.15f);
            m2.localRotation = Quaternion.Euler(90, 0, 0);

            stationItem = brickItem.Clone(10542, "Placeable_BatteryStation");
            stationItem.name = stationItem.UniqueName;
            stationItem.settings_Inventory.LocalizationTerm = "Item/Placeable_BatteryStation";
            stationItem.settings_Inventory.Sprite = LoadImage("BatteryStation.png").ToSprite();
            Traverse.Create(stationItem.settings_Inventory).Field("stackable").SetValue(false);
            stationItem.SetRecipe(new[]
                {
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(73) }, 2),
                    new CostMultiple(new[] { plankItem }, 6),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(177) }, 4),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(125) }, 4),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(178) }, 16),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(154) }, 10)
                }, CraftingCategory.Other, 1, false, "Cabling", 2);

            var stationPrefab = Instantiate(stationItem.settings_buildable.GetBlockPrefab(0), prefabHolder, false);
            stationPrefab.name = "Block_BatteryStation";
            var s = stationPrefab.gameObject.AddComponent<Block_BatteryStation>();
            s.CopyFieldsOf(stationPrefab);
            s.ReplaceValues(stationPrefab, s);
            stationPrefab.ReplaceValues(brickItem, stationItem);
            DestroyImmediate(stationPrefab);
            stationPrefab = s;
            DestroyImmediate(stationPrefab.GetComponent<Brick_Wet>());
            DestroyImmediate(stationPrefab.GetComponent<RaycastInteractable>());
            stationPrefab.transform.Find("model").transform.localScale = new Vector3(1.5f,1,2);
            m = new GameObject("model (2)");
            m.transform.SetParent(stationPrefab.transform, false);
            m2 = pillarItem.settings_buildable.GetBlockPrefab(0).transform.GetChild(0);
            m.AddComponent<MeshFilter>().sharedMesh = m2.GetComponent<MeshFilter>().sharedMesh;
            m.AddComponent<MeshRenderer>().material = m2.GetComponent<MeshRenderer>().material;
            m.transform.localScale = new Vector3(1.5f, 1.45f, 1.5f);
            for (int i = 0; i < 4; i++)
            {
                m = new GameObject($"model ({i + 3})");
                m.transform.SetParent(stationPrefab.transform, false);
                m2 = pillarItem.settings_buildable.GetBlockPrefab(0).transform.GetChild(0);
                m.AddComponent<MeshFilter>().sharedMesh = m2.GetComponent<MeshFilter>().sharedMesh;
                m.AddComponent<MeshRenderer>().material = m2.GetComponent<MeshRenderer>().material;
                m.transform.localScale = new Vector3(0.4f, 0.25f, 0.4f);
                m.transform.localRotation = Quaternion.Euler(0, i % 2 == 1 ? -90 : 0, 90);
                m.transform.localPosition = Quaternion.Euler(0, i % 2 == 1 ? -90 : 0, 0) * new Vector3(0.15f, 0.3f + 0.6f * (i / 2), 0);
            }
            c = stationPrefab.GetComponent<BoxCollider>();
            c.size = new Vector3(0.5f, 1.5f, 0.5f);
            c.center = new Vector3(0, c.size.y / 2, 0);
            s.station = s.gameObject.AddComponent<BatteryStation>();
            s.station.batteries = new Battery[16];
            for (int i = 0; i < 16; i++)
            {
                var a = Instantiate(batteryPrefab, stationPrefab.transform, false).GetComponent<BatteryAccess>();
                a.networkBehaviourID = s.station;
                a.batteryIndex = i;
                a.transform.localRotation = Quaternion.Euler(0, 45 * (i / 2), 0);
                a.transform.localPosition = a.transform.localRotation * new Vector3(0, 0.6f * (i % 2) + 0.4f, -0.4f);
                a.transform.localScale = a.transform.parent.transform.InverseTransformDirection(batteryPrefab.transform.localScale);
                s.station.batteries[i] = a.GetComponent<Battery>();
            }
            stationPrefab.networkType = NetworkType.NetworkIDBehaviour;
            stationPrefab.networkedIDBehaviour = s.station;
            Traverse.Create(stationItem.settings_buildable).Field("blockPrefabs").SetValue(new[] { stationPrefab });

            Instantiate(connectorPrefab, stationPrefab.transform, false).transform.localPosition = new Vector3(0, 1.9f, 0);

            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (q.AcceptsBlock(brickItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().AddRange(new[] { stationItem, jointItem });
                else if (q.AcceptsBlock(ziplineItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(jointItem);
            RAPI.RegisterItem(stationItem);
            RAPI.RegisterItem(jointItem);
            RAPI.RegisterItem(batteryItem);

            (harmony = new Harmony("com.aidanamite.WireBatteries")).PatchAll();
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();

            Log("Mod has been loaded!");
        }

        public void OnModUnload()
        {
            if (!loaded)
                return;
            loaded = false;
            harmony?.UnpatchAll(harmony.Id);
            LocalizationManager.Sources.Remove(language);
            ItemManager.GetAllItems().RemoveAll(x => x.UniqueIndex == stationItem?.UniqueIndex || x.UniqueIndex == jointItem?.UniqueIndex || x.UniqueIndex == batteryItem?.UniqueIndex);
            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().RemoveAll(x => x.UniqueIndex == stationItem?.UniqueIndex || x.UniqueIndex == jointItem?.UniqueIndex);
            foreach (var battery in Patch_Battery.batteries)
                if (battery && !battery.GetComponent<NoCableBattery>())
                {
                    var bats = battery.itemEnabler.GetObjectConnections().ToList();
                    foreach (var x in bats.ToArray())
                        if (x.item.UniqueIndex == batteryItem?.UniqueIndex)
                        {
                            Destroy(x.model);
                            bats.Remove(x);
                        }
                    Traverse.Create(battery.itemEnabler).Field("itemConnections").SetValue(bats.ToArray());
                    if (battery.GetBatteryInstance()?.UniqueIndex == batteryItem?.UniqueIndex)
                        Traverse.Create(battery).Field("batteryInstance").SetValue(null);
                }
            foreach (var block in BlockCreator.GetPlacedBlocks().ToArray())
                if (block.buildableItem?.UniqueIndex == stationItem?.UniqueIndex || block.buildableItem?.UniqueIndex == jointItem?.UniqueIndex)
                    BlockCreator.RemoveBlock(block, null, false);
            Destroy(prefabHolder?.gameObject);
            Log("Mod has been unloaded!");
        }

        public Texture2D LoadImage(string filename, bool leaveReadable = false)
        {
            var t = new Texture2D(0, 0, UnityEngine.Experimental.Rendering.GraphicsFormat.B8G8R8A8_SRGB, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
            t.LoadImage(GetEmbeddedFileBytes(filename),!leaveReadable);
            if (leaveReadable)
                t.Apply();
            return t;
        }


        public static Action<Battery> EditBattery;
    }

    class Block_CableConnector : Block { }

    class Block_BatteryStation : Block_CableConnector
    {
        public BatteryStation station;
        public override RGD Serialize_Save() => new RGD_Block(RGDType.Block, this);
    }

    class BatteryStation : MonoBehaviour_ID_Network
    {
        public Battery[] batteries;
        public MeshPathBase_RaftElectricity pathBase;
        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            switch (msg.Type)
            {
                case Messages.Battery_Insert:
                case Messages.Battery_Take:
                case Messages.Battery_Update:
                case Messages.Battery_OnOff:
                    var message = msg as Message_Battery;
                    if (message != null)
                    {
                        var f = batteries[message.batteryIndex].OnBatteryMessage(msg);
                        foreach (var b in GetComponentsInChildren<MeshPathBase_RaftElectricity>())
                            b.network?.UpdateAllStates();
                        return f;
                    }
                    break;
            }
            return base.Deserialize(msg, remoteID);
        }
        bool placed = false;
        void OnBlockPlaced()
        {
            placed = true;
            NetworkIDManager.AddNetworkID(this);
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (placed)
                NetworkIDManager.RemoveNetworkID(this);
        }
        static ConditionalWeakTable<Battery, BatteryStation> table = new ConditionalWeakTable<Battery, BatteryStation>();
        public static BatteryStation GetInstanceForBattery(Battery battery)
        {
            if (!battery)
                return null;
            if (!table.TryGetValue(battery, out var path) || !path)
            {
                table.Remove(battery);
                table.Add(battery, path = battery.GetComponentInParent<BatteryStation>());
            }
            return (object)path != null ? path : null;
        }
    }



    class MeshPathBase_RaftElectricity : MeshPathBase
    {
        Network_Player player;
        public ElectricityNetwork network;
        public BatteryStation station;
        public bool hasStation;
        public Battery battery;
        void OnDisable()
        {
            if (!player)
                player = ComponentManager<Network_Player>.Value;
            if (Raft_Network.IsHost)
                for (int i = MeshPath.PathConnections.Count - 1; i >= 0; i--)
                    if (MeshPath.PathConnections[i].baseA == this || MeshPath.PathConnections[i].baseB == this)
                    {
                        var msg = new Message_MeshPath(Messages.MeshPath_Remove, player.Network.NetworkIDManager, MeshPath.PathConnections[i].baseA, MeshPath.PathConnections[i].baseB, 0f);
                        if (MeshPath.RemovePath(MeshPath.PathConnections[i].path))
                            player.Network.RPC(msg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                    }
            NetworkIDManager.RemoveNetworkID(this, typeof(MeshPathBase));
        }

        void OnEnable()
        {
            hasStation = station = GetComponentInParent<BatteryStation>();
            if (hasStation)
                station.pathBase = this;
            battery = GetComponentInParent<Battery>();
            NetworkIDManager.AddNetworkID(this, typeof(MeshPathBase));
            if (battery && battery.On)
                battery.On = false;
        }

        public void Toggle(bool hasPower)
        {
            if (battery)
            {
                if (battery.On && !hasPower)
                    battery.On = false;
                battery.UpdateBatteryHolderLights();
            }
        }

        public void AssignNewId() => ObjectIndex = SaveAndLoad.GetUniqueObjectIndex();

        static ConditionalWeakTable<Battery, MeshPathBase_RaftElectricity> table = new ConditionalWeakTable<Battery, MeshPathBase_RaftElectricity>();
        public static MeshPathBase_RaftElectricity GetInstanceForBattery(Battery battery)
        {
            if (!battery)
                return null;
            if (!table.TryGetValue(battery, out var path) || !path)
            {
                table.Remove(battery);
                table.Add(battery, path = battery.GetComponentInChildren<MeshPathBase_RaftElectricity>(true));
            }
            return path && path.isActiveAndEnabled ? path : null;
        }
    }

    static class SaveHandler
    {
        public const short OwnId = 0;
        public static RGDType SubId = new RGD(~RGDType.None, default).Type;
        public static Dictionary<uint, RGD_Slot> waiting = new Dictionary<uint, RGD_Slot>();
        public static RGD_Block CreateSave(params uint[] specificObjects)
        {
            var d = CreateObject<RGD_Storage>();
            d.Type = SubId;
            var l = new List<RGD_Slot>();
            l.Add(CreateObject<RGD_Slot>());
            l[0].slotIndex = OwnId;
            foreach (var o in Object.FindObjectsOfType<MonoBehaviour_ID>())
            {
                if (specificObjects.Length > 0 && !specificObjects.Contains(o.ObjectIndex))
                    continue;
                var r = CreateFor(o);
                if (r != null)
                {
                    var b = BitConverter.GetBytes(o.ObjectIndex);
                    r.slotIndex = BitConverter.ToInt16(b, 0);
                    r.itemIndex = BitConverter.ToInt16(b, 2);
                    l.Add(r);
                }
            }
            d.slots = l.ToArray();
            return d;
        }
        public static bool Load(RGD_Block data)
        {
            var d = data as RGD_Storage;
            if (d?.slots != null && d.slots.Length > 0 && d.slots[0].slotIndex == OwnId && d.Type == SubId)
            {
                var o = Resources.FindObjectsOfTypeAll<MonoBehaviour_ID>();
                var skip = true;
                foreach (var r in d.slots)
                {
                    if (skip)
                    {
                        skip = false;
                        continue;
                    }
                    var b = new byte[4];
                    BitConverter.GetBytes(r.slotIndex).CopyTo(b, 0);
                    BitConverter.GetBytes(r.itemIndex).CopyTo(b, 2);
                    var i = BitConverter.ToUInt32(b, 0);
                    if (i == 0)
                        continue;
                    var k = o.FirstOrDefault(x => x is Block && x.ObjectIndex == i);
                    if (k == null)
                        waiting[i] = r;
                    else
                        LoadOnto(k, r);
                }
                return true;
            }
            return false;
        }

        public static RGD_Slot CreateFor(MonoBehaviour_ID obj)
        {
            var j = new JSONObject(new Dictionary<string,JSONObject>());
            var i = (obj as Block)?.GetComponentsInChildren<MeshPathBase_RaftElectricity>(true);
            if (i != null && i.Length > 0)
            {
                var c = new List<JSONObject>();
                foreach (var k in i)
                    c.Add(JSONObject.CreateStringObject(k.ObjectIndex.ToString()));
                j.AddField("PathIndecies", new JSONObject(c.ToArray()));
            }
            var b = (obj as Block_BatteryStation)?.station?.batteries;
            if (b != null)
            {
                var c = new List<JSONObject>();
                foreach (var k in b)
                    if (k.BatterySlotIsEmpty)
                        c.Add(new JSONObject(false));
                    else
                        c.Add(JSONObject.CreateStringObject(JsonUtility.ToJson(new RGD_Battery(k))));
                j.AddField("Batteries", new JSONObject(c.ToArray()));
            }
            if (j.Count == 0)
                return null;
            var e = CreateObject<RGD_Slot>();
            e.exclusiveString = j.ToString();
            return e;
        }
        public static void LoadOnto(MonoBehaviour_ID obj, RGD_Slot rgd)
        {
            var j = new JSONObject(rgd.exclusiveString);
            var e = j.HasField("PathIndecies") ? j.GetField("PathIndecies") : null;
            if (e != null && e.IsArray)
            {
                var c = e.list.ToArray();
                var i = (obj as Block)?.GetComponentsInChildren<MeshPathBase_RaftElectricity>(true);
                if (i != null)
                    for (int k = 0; k < Mathf.Min(i.Length, c.Length); k++)
                        i[k].ObjectIndex = uint.Parse(c[k].str);
            }
            e = j.HasField("Batteries") ? j.GetField("Batteries") : null;
            if (e != null && e.IsArray)
            {
                var c = e.list.ToArray();
                var i = (obj as Block_BatteryStation)?.station?.batteries;
                if (i != null)
                    for (int k = 0; k < Mathf.Min(i.Length, c.Length); k++)
                        if (c[k].IsString)
                            JsonUtility.FromJson<RGD_Battery>(c[k].str).RestoreBattery(i[k]);
            }
        }

        public static T CreateObject<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    }

    class ElectricityNetwork
    {
        public List<MeshPathConnection> connections = new List<MeshPathConnection>();
        public List<MeshPathBase_RaftElectricity> bases = new List<MeshPathBase_RaftElectricity>();
        void Merge(ElectricityNetwork other)
        {
            foreach (var i in other.bases)
                i.network = this;
            bases.AddRange(other.bases);
            connections.AddRange(other.connections);
            cache = null;
        }

        (int current, int max)? cache;
        public int GetPower(out int maxCapacity)
        {
            if (cache != null)
            {
                maxCapacity = cache.Value.max;
                return cache.Value.current;
            }
            var max = 0;
            var sum = 0;
            foreach (var bas in bases)
                if (bas.hasStation)
                    foreach(var bat in bas.station.batteries)
                        if (!bat.BatterySlotIsEmpty)
                        {
                            var i = bat.GetBatteryInstance();
                            max += i.BaseItemMaxUses;
                            sum += i.Uses;
                        }
            cache = (sum, maxCapacity = max);
            return sum;
        }
        public bool HasPower()
        {
            foreach (var bas in bases)
                if (bas.hasStation)
                    foreach (var bat in bas.station.batteries)
                        if (bat.CanGiveElectricity)
                            return true;
            return false;
        }
        public bool ChangePower(int amount)
        {
            cache = null;
            foreach (var bas in bases)
                if (bas.hasStation)
                    foreach (var bat in bas.station.batteries)
                        if (!bat.BatterySlotIsEmpty)
                        {
                            var r = bat.BatteryUses;
                            if (amount > 0)
                            {
                                var m = bat.GetBatteryInstance().BaseItemMaxUses;
                                if (m - r < amount)
                                {
                                    bat.AddBatteryUsesNetworked(m - r);
                                    amount -= m - r;
                                    continue;
                                }
                                else
                                    bat.AddBatteryUsesNetworked(amount);
                                return true;
                            }
                            else
                            {
                                if (bat.CanGiveElectricity)
                                {
                                    if (r <= -amount)
                                    {
                                        bat.RemoveBatteryUsesNetworked(r);
                                        amount += r;
                                        continue;
                                    }
                                    else if (amount < 0)
                                        bat.RemoveBatteryUsesNetworked(-amount);
                                    return true;
                                }
                            }
                        }
            return amount > 0;
        }

        public void UpdateAllStates()
        {
            cache = null;
            bool flag = HasPower();
            foreach (var b in bases)
                b.Toggle(flag);
        }

        public static void OnConnected(MeshPathBase_RaftElectricity baseA, MeshPathBase_RaftElectricity baseB, MeshPathConnection pathConnection)
        {
            if (!baseA || !baseB)
                return;
            if (baseA.network == null && baseB.network == null)
            {
                var net = new ElectricityNetwork() { bases = new List<MeshPathBase_RaftElectricity> { baseA, baseB }, connections = new List<MeshPathConnection> { pathConnection } };
                baseA.network = net;
                baseB.network = net;
                net.UpdateAllStates();
            }
            else if (baseA.network == null)
            {
                baseB.network.bases.Add(baseA);
                baseB.network.connections.Add(pathConnection);
                baseB.network.cache = null;
                baseA.network = baseB.network;
                baseA.network.UpdateAllStates();
            }
            else if (baseB.network == null)
            {
                baseA.network.bases.Add(baseB);
                baseA.network.connections.Add(pathConnection);
                baseA.network.cache = null;
                baseB.network = baseA.network;
                baseA.network.UpdateAllStates();
            }
            else if (baseA.network != baseB.network)
            {
                baseA.network.Merge(baseB.network);
                baseA.network.connections.Add(pathConnection);
                baseA.network.UpdateAllStates();
            }
            else
                baseA.network.connections.Add(pathConnection);
        }

        public static void OnDisconnect(MeshPath_RaftElectricity path)
        {
            if (!path)
                return;
            foreach (var c in MeshPath.PathConnections)
                if (c.path == path)
                {
                    OnDisconnect(c);
                    break;
                }
        }
        public static void OnDisconnect(MeshPathConnection connection)
        {
            if (connection == null)
                return;
            var baseA = connection.baseA as MeshPathBase_RaftElectricity;
            var baseB = connection.baseB as MeshPathBase_RaftElectricity;
            if (!baseA || !baseB)
                return;
            var newConnections = new List<MeshPathConnection>();
            var newBases = new List<MeshPathBase_RaftElectricity>() { baseB };
            var changes = 1;
            while (changes != 0)
            {
                changes = 0;
                foreach (var c in baseB.network.connections)
                {
                    if (c == connection || newConnections.Contains(c))
                        continue;
                    if (newBases.Contains(c.baseA))
                    {
                        if (baseA == c.baseB)
                            goto loopEnd;
                        newBases.Add(c.baseB as MeshPathBase_RaftElectricity);
                        newConnections.Add(c);
                        changes++;
                    } else if (newBases.Contains(c.baseB))
                    {
                        if (baseA == c.baseA)
                            goto loopEnd;
                        newBases.Add(c.baseA as MeshPathBase_RaftElectricity);
                        newConnections.Add(c);
                        changes++;
                    }
                }
            }
            var n = new ElectricityNetwork() { bases = newBases, connections = newConnections };
            foreach (var b in newBases)
            {
                b.network = n;
                baseA.network.bases.Remove(b);
            }
            foreach (var c in newConnections)
                baseA.network.connections.Remove(c);
            baseB.network.UpdateAllStates();
            if (n.connections.Count == 0)
                baseB.network = null;
        loopEnd:
            baseA.network.cache = null;
            baseA.network.connections.Remove(connection);
            baseA.network.UpdateAllStates();
            if (baseA.network.connections.Count == 0)
                baseA.network = null;
        }
    }

    class MeshPathBaseParent : MonoBehaviour
    {
        Transform parent;
        void Awake()
        {
            parent = GetComponentInParent<Block>()?.transform;
            Update();
        }
        public void Update()
        {
            var r = parent?.parent?.rotation;
            if (r != null && r.Value != transform.rotation)
            {
                var l = new List<(Transform, Quaternion)>();
                foreach (Transform t in transform)
                    if (t.GetComponent<MeshPath>())
                        l.Add((t, t.rotation));
                transform.rotation = r.Value;
                foreach (var t in l)
                    t.Item1.rotation = t.Item2;
            }
        }
    }

    class MeshPath_RaftElectricity : MeshPath, IRaycastable
    {
        Network_Player player;
        CanvasHelper canvas;
        void Start()
        {
            player = ComponentManager<Network_Player>.Value;
            canvas = ComponentManager<CanvasHelper>.Value;
        }
        void IRaycastable.OnIsRayed()
        {
            if (player == null || PlayerItemManager.IsBusy || CanvasHelper.ActiveMenu != MenuType.None)
                return;
            canvas.displayTextManager.ShowText(Helper.GetTerm("Game/Remove", false), MyInput.Keybinds["Remove"].MainKey, 0, 0, true);
            if (MyInput.GetButtonDown("Remove"))
            {
                canvas.displayTextManager.HideDisplayTexts();
                MeshPathConnection blockConnection = GetBlockConnection(this);
                if (blockConnection != null)
                {
                    Message_MeshPath message = new Message_MeshPath(Messages.MeshPath_Remove, player.Network.NetworkIDManager, blockConnection.baseA, blockConnection.baseB, 0f);
                    if (Raft_Network.IsHost)
                    {
                        if (RemovePath(this))
                            player.Network.RPC(message, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                    }
                    else
                        player.SendP2P(message, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                }
            }
        }
        void IRaycastable.OnRayEnter() { }

        void IRaycastable.OnRayExit()
        {
            if (canvas)
                canvas.displayTextManager.HideDisplayTexts();
        }
        void OnDestroy() => ElectricityNetwork.OnDisconnect(this);
    }

    class BatteryAccess : MonoBehaviour
    {
        static FieldInfo _networkBehaviourID = typeof(Battery).GetField("networkBehaviourID", ~BindingFlags.Default);
        static FieldInfo _batteryIndex = typeof(Battery).GetField("batteryIndex", ~BindingFlags.Default);
        public MonoBehaviour_ID_Network networkBehaviourID
        {
            get => (MonoBehaviour_ID_Network)_networkBehaviourID.GetValue(GetComponent<Battery>());
            set => _networkBehaviourID.SetValue(GetComponent<Battery>(),value);
        }
        public int batteryIndex
        {
            get => (int)_batteryIndex.GetValue(GetComponent<Battery>());
            set => _batteryIndex.SetValue(GetComponent<Battery>(), value);
        }
    }
    class NoCableBattery : MonoBehaviour { }

    static class ExtentionMethods
    {
        public static Item_Base Clone(this Item_Base source, int uniqueIndex, string uniqueName)
        {
            Item_Base item = ScriptableObject.CreateInstance<Item_Base>();
            item.Initialize(uniqueIndex, uniqueName, source.MaxUses);
            item.settings_buildable = source.settings_buildable.Clone();
            item.settings_consumeable = source.settings_consumeable.Clone();
            item.settings_cookable = source.settings_cookable.Clone();
            item.settings_equipment = source.settings_equipment.Clone();
            item.settings_Inventory = source.settings_Inventory.Clone();
            item.settings_recipe = source.settings_recipe.Clone();
            item.settings_usable = source.settings_usable.Clone();
            return item;
        }
        public static void SetRecipe(this Item_Base item, CostMultiple[] cost, CraftingCategory category = CraftingCategory.Resources, int amountToCraft = 1, bool learnedFromBeginning = false, string subCategory = null, int subCatergoryOrder = 0)
        {
            Traverse recipe = Traverse.Create(item.settings_recipe);
            recipe.Field("craftingCategory").SetValue(category);
            recipe.Field("amountToCraft").SetValue(amountToCraft);
            recipe.Field("learnedFromBeginning").SetValue(learnedFromBeginning);
            recipe.Field("subCategory").SetValue(subCategory);
            recipe.Field("subCatergoryOrder").SetValue(subCatergoryOrder);
            item.settings_recipe.NewCost = cost;
        }

        public static void CopyFieldsOf(this object value, object source)
        {
            var t1 = value.GetType();
            var t2 = source.GetType();
            while (!t1.IsAssignableFrom(t2))
                t1 = t1.BaseType;
            while (t1 != typeof(Object) && t1 != typeof(object))
            {
                foreach (var f in t1.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                        f.SetValue(value, f.GetValue(source));
                t1 = t1.BaseType;
            }
        }

        public static void ReplaceValues(this Component value, object original, object replacement)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement);
        }
        public static void ReplaceValues(this GameObject value, object original, object replacement)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement);
        }

        public static void ReplaceValues(this object value, object original, object replacement)
        {
            var t = value.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic && f.GetValue(value) == original)
                        f.SetValue(value, replacement);
                t = t.BaseType;
            }
        }

        public static Sprite ToSprite(this Texture2D texture, Rect? rect = null, Vector2? pivot = null) => Sprite.Create(texture, rect ?? new Rect(0, 0, texture.width, texture.height), pivot ?? new Vector2(0.5f, 0.5f));
    
        public static bool GetPower(this Battery battery,out int current,out int max)
        {
            current = 0;
            max = 0;
            if (!battery.BatterySlotIsEmpty && battery.GetBatteryInstance().UniqueIndex == Main.instance.batteryItem.UniqueIndex)
            {
                var n = MeshPathBase_RaftElectricity.GetInstanceForBattery(battery)?.network;
                if (n != null)
                    current = n.GetPower(out max);
                return true;
            }
            return false;
        }

        static MethodInfo _UpdateBatteryHolderLights = typeof(Battery).GetMethod("UpdateBatteryHolderLights", ~BindingFlags.Default);
        public static void UpdateBatteryHolderLights(this Battery battery) => _UpdateBatteryHolderLights.Invoke(battery, new object[0]);
    }

    [HarmonyPatch(typeof(ZiplinePlayer),"Update")]
    static class Patch_ZiplinePlacement
    {
        public static bool call = false;
        static void Prefix() => call = true;
        static void Postfix() => call = false;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var loc5 = code.Find(x => x.opcode == OpCodes.Stloc_S && (x.operand as LocalBuilder).LocalIndex == 5).operand;
            var loc6 = code.Find(x => x.opcode == OpCodes.Stloc_S && (x.operand as LocalBuilder).LocalIndex == 6).operand;
            code.InsertRange(code.FindIndex(x => x.opcode == OpCodes.Ldsfld && (x.operand as FieldInfo).Name == "MeshPathCreationIsObstructed") + 1, new[] {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Ldloc_S, loc5),
                new CodeInstruction(OpCodes.Ldloc_S, loc6),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_ZiplinePlacement),nameof(SkipCollisionCheck)))
            });
            code.InsertRange(code.FindIndex(x => x.opcode == OpCodes.Ldfld && (x.operand as FieldInfo).Name == "minDistanceFromGround") + 1, new[] {
                new CodeInstruction(OpCodes.Ldloc_3),
                new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(Patch_ZiplinePlacement), nameof(Process)))
            });
            return code;
        }
        static bool SkipCollisionCheck(bool alreadySkipped, MeshPathBase startBase, Vector3 start, Vector3 end)
        {
            if (alreadySkipped || !(startBase is MeshPathBase_RaftElectricity))
                return alreadySkipped;
            return (start - end).sqrMagnitude <= 0.2;
        }
        static float Process(float defaultValue, MeshPathBase startBase)
        {
            if (startBase is MeshPathBase_RaftElectricity)
                return 0.05f;
            return defaultValue;
        }
    }

    [HarmonyPatch(typeof(MeshPath), "DoesConnectionExist")]
    static class Patch_CannotConnect
    {
        static void Postfix(MeshPathBase baseA, MeshPathBase baseB, ref bool __result)
        {
            if (!__result && Patch_ZiplinePlacement.call && baseA && baseB)
                __result = baseA is MeshPathBase_RaftElectricity != baseB is MeshPathBase_RaftElectricity;
        }
    }

    [HarmonyPatch(typeof(Battery))]
    static class Patch_Battery
    {
        public static HashSet<Battery> batteries = new HashSet<Battery>();


        [HarmonyPatch("OnRayEnter")]
        [HarmonyPrefix]
        static void OnRayEnter(Battery __instance)
        {
            if (batteries.Add(__instance) && !__instance.GetComponent<NoCableBattery>())
            {
                Main.EditBattery(__instance);
                foreach (var p in __instance.GetComponentsInChildren<MeshPathBase_RaftElectricity>(true))
                    if (p.ObjectIndex == 0)
                        p.AssignNewId();
            }
        }


        [HarmonyPatch("NormalizedBatteryLeft", MethodType.Getter)]
        [HarmonyPostfix]
        static void NormalizedBatteryLeft(Battery __instance, ref float __result)
        {
            if (__instance.GetPower(out var r, out var m))
                __result = m == 0 ? 0 : ((float)r / m);
        }

        [HarmonyPatch("CanGiveElectricity", MethodType.Getter)]
        [HarmonyPostfix]
        static void CanGiveElectricity(Battery __instance, ItemInstance ___batteryInstance, ref bool __result)
        {
            if (!__instance.BatterySlotIsEmpty && ___batteryInstance.UniqueIndex == Main.instance.batteryItem.UniqueIndex)
                __result = MeshPathBase_RaftElectricity.GetInstanceForBattery(__instance)?.network?.HasPower() ?? false;
        }

        [HarmonyPatch("Update", typeof(int))]
        [HarmonyPrefix]
        static bool Update(Battery __instance, ItemInstance ___batteryInstance, int batteryUses)
        {
            if (!__instance.BatterySlotIsEmpty && ___batteryInstance.UniqueIndex == Main.instance.batteryItem.UniqueIndex)
            {
                if (Raft_Network.IsHost)
                {
                    if (!(MeshPathBase_RaftElectricity.GetInstanceForBattery(__instance)?.network?.ChangePower(batteryUses - ___batteryInstance.Uses) ?? false))
                        __instance.On = false;
                }
                __instance.UpdateBatteryHolderLights();
                return false;
            }
            return true;
        }

        [HarmonyPatch("Update", typeof(int))]
        [HarmonyPostfix]
        static void Update_post(Battery __instance)
        {
            BatteryStation.GetInstanceForBattery( __instance)?.pathBase?.network?.UpdateAllStates();
        }

        [HarmonyPatch("UpdateBatteryHolderLights")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> UpdateBatteryHolderLights(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind2 = code.FindIndex(x => x.opcode == OpCodes.Callvirt && (x.operand as MethodInfo).Name == "get_NormalizedUses");
            var ind = code.FindLastIndex(ind2, x => x.opcode == OpCodes.Ldarg_0) + 1;
            code.RemoveRange(ind, ind2 - ind + 1);
            code.Insert(ind, new CodeInstruction(OpCodes.Callvirt, AccessTools.Property(typeof(Battery), nameof(Battery.NormalizedBatteryLeft)).GetMethod));
            return code;
        }

        [HarmonyPatch("BatteryUses",MethodType.Getter)]
        [HarmonyPostfix]
        static void BatteryUses(Battery __instance, ItemInstance ___batteryInstance, ref int __result)
        {
            if (!__instance.BatterySlotIsEmpty
                && ___batteryInstance.UniqueIndex == Main.instance.batteryItem.UniqueIndex
                && !Patch_ReturnItemsFromBlock.calling
                && !Patch_RGDBattery.calling)
            {
                __result = MeshPathBase_RaftElectricity.GetInstanceForBattery(__instance)?.network?.GetPower(out _) ?? 0;
            }
        }

        [HarmonyPatch("Take")]
        [HarmonyPostfix]
        static void Take(Battery __instance, bool __result)
        {
            if (__result)
                BatteryStation.GetInstanceForBattery(__instance)?.pathBase?.network?.UpdateAllStates();
        }

        [HarmonyPatch("Insert")]
        [HarmonyPostfix]
        static void Insert(Battery __instance, bool __result)
        {
            if (__result)
                BatteryStation.GetInstanceForBattery(__instance)?.pathBase?.network?.UpdateAllStates();
        }
    }
    [HarmonyPatch(typeof(RGD_Battery), MethodType.Constructor, typeof(Battery))]
    static class Patch_RGDBattery
    {
        public static bool calling = false;
        static void Prefix() => calling = true;
        static void Postfix()=>calling = false;
    }

    [HarmonyPatch(typeof(RemovePlaceables), "ReturnItemsFromBlock")]
    static class Patch_ReturnItemsFromBlock
    {
        public static bool calling = false;
        static void Prefix() => calling = true;
        static void Postfix(Block block, Network_Player player, bool giveItems)
        {
            if (giveItems && block.buildableItem.UniqueIndex == Main.instance.stationItem.UniqueIndex)
            {
                var b = block?.GetComponent<BatteryStation>()?.batteries;
                if (b != null)
                    foreach (var battery in b)
                        if (!battery.BatterySlotIsEmpty)
                            battery.Take(player, battery.BatteryUses);
            }
            calling = false;
        }
    }

    [HarmonyPatch(typeof(SaveAndLoad), "RestoreRGDGame")]
    static class Patch_LoadGame
    {
        public static bool calling = false;
        static bool Prefix() => calling = true;
        static void Postfix()
        {
            calling = false;
            foreach (var b in BlockCreator.GetPlacedBlocks())
                foreach (var p in b.GetComponentsInChildren<MeshPathBase_RaftElectricity>(true))
                    if (p.ObjectIndex == 0)
                        p.AssignNewId();
        }
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.opcode == OpCodes.Call && (x.operand as MethodInfo).Name == "get_Value" && (x.operand as MethodInfo).DeclaringType == typeof(ComponentManager<RaftBounds>));
            var labels = code[ind].labels;
            code[ind].labels = new List<Label>();
            code.InsertRange(ind, new[] {
                new CodeInstruction(OpCodes.Ldarg_1) {labels = labels},
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_LoadGame), nameof(OnLoad)))
            });
            return code;
        }
        static void OnLoad(RGD_Game game)
        {
            if (game.behaviours != null)
                foreach (var b in game.behaviours)
                    try
                    {
                        if (SaveHandler.Load(b as RGD_Block))
                            break;
                    } catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
        }
    }
    [HarmonyPatch(typeof(SaveAndLoad), "CreateRGDGame")]
    static class Patch_SaveGame
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.opcode == OpCodes.Call && (x.operand as MethodInfo).Name == "get_Value" && (x.operand as MethodInfo).DeclaringType == typeof(ComponentManager<Network_Host>));
            var labels = code[ind].labels;
            code[ind].labels = new List<Label>();
            code.InsertRange(ind, new[] {
                new CodeInstruction(OpCodes.Ldloc_0) {labels = labels},
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_SaveGame), nameof(OnSave)))
            });
            return code;
        }
        static void OnSave(RGD_Game game)
        {
            try
            {
                game.behaviours?.Add(SaveHandler.CreateSave());
            } catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
    [HarmonyPatch(typeof(Raft_Network), "GetWorld")]
    static class Patch_SendWorld
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindLastIndex(x => x.opcode == OpCodes.Ldloc_0);
            var loc5 = code.Find(x => x.opcode == OpCodes.Ldloc_S && (x.operand as LocalBuilder).LocalIndex == 5).operand;
            var labels = code[ind].labels;
            code[ind].labels = new List<Label>();
            code.InsertRange(ind, new[] {
                new CodeInstruction(OpCodes.Ldloc_0) {labels = labels},
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_SendWorld), nameof(OnSend)))
            });
            return code;
        }
        static void OnSend(List<Message> game)
        {
            try { 
                var msg = SaveHandler.CreateObject<Message_BlockCreator_Create>();
                msg.Type = ~Messages.NOTHING;
                msg.ObjectIndex = 0;
                msg.rgdBlocks = new[] { SaveHandler.CreateSave() };
                var i = game.FindIndex(x => x.Type == Messages.MeshPath_CreateAll);
                game.Insert(i == -1 ? game.Count - 1 : i,msg);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
    [HarmonyPatch(typeof(NetworkUpdateManager), "Deserialize")]
    static class Patch_RecieveWorld
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.opcode == OpCodes.Stloc_2);
            code.InsertRange(ind, new[] {
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_RecieveWorld), nameof(OnRecieve)))
            });
            return code;
        }
        static Message OnRecieve(Message msg)
        {
            var message = msg as Message_BlockCreator_Create;
            try {
                if (message != null && msg.Type == ~Messages.NOTHING && message.ObjectIndex == 0 && message.rgdBlocks?.Length == 1 && SaveHandler.Load(message.rgdBlocks[0]))
                    return null;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            return msg;
        }
    }


    [HarmonyPatch(typeof(MeshPath), "CreateMeshPath")]
    static class Patch_CreateMeshPath
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var ind = code.FindIndex(x => x.opcode == OpCodes.Callvirt && (x.operand as MethodInfo).Name == "Add") + 1;
            code.InsertRange(ind, new[] {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_CreateMeshPath), nameof(OnConnect)))
            });
            return code;
        }
        static void OnConnect(MeshPathBase a, MeshPathBase b, MeshPathConnection c)
        {
            ElectricityNetwork.OnConnected(a as MeshPathBase_RaftElectricity, b as MeshPathBase_RaftElectricity, c);
        }
    }

    [HarmonyPatch(typeof(Block))]
    static class Patch_Block
    {
        [HarmonyPatch("OnStartingPlacement")]
        [HarmonyPrefix]
        public static void OnStartingPlacement(Block __instance)
        {
            foreach (var b in __instance.GetComponentsInChildren<Battery>(true))
                if (Patch_Battery.batteries.Add(b) && !b.GetComponent<NoCableBattery>())
                    Main.EditBattery(b);
        }

        [HarmonyPatch("OnFinishedPlacement")]
        [HarmonyPostfix]
        static void OnFinishedPlacement(Block __instance)
        {
            foreach (var b in __instance.GetComponentsInChildren<MeshPathBase_RaftElectricity>(true))
            {
                if (!Patch_LoadGame.calling && b.ObjectIndex == 0)
                    if (Raft_Network.IsHost)
                    b.AssignNewId();
                b.GetComponent<Collider>().enabled = true;
            }
            foreach (var b in __instance.GetComponentsInChildren<MeshPathBaseParent>(true))
                b.Update();
            if (Raft_Network.IsHost)
            {
                var msg = SaveHandler.CreateObject<Message_BlockCreator_Create>();
                msg.Type = ~Messages.NOTHING;
                msg.ObjectIndex = 0;
                msg.rgdBlocks = new[] { SaveHandler.CreateSave(__instance.ObjectIndex) };
                ComponentManager<Raft_Network>.Value.RPC(msg, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
            }
            else if (SaveHandler.waiting.TryGetValue(__instance.ObjectIndex, out var r))
                SaveHandler.LoadOnto(__instance, r);

        }
    }

    [HarmonyPatch(typeof(MeshPath), "RemovePath")]
    static class Patch_MeshPath
    {
        static void Prefix(MeshPath path) => ElectricityNetwork.OnDisconnect(path as MeshPath_RaftElectricity);
    }

    [HarmonyPatch(typeof(LanguageSourceData), "GetLanguageIndex")]
    static class Patch_GetLanguageIndex
    {
        static void Postfix(LanguageSourceData __instance, ref int __result)
        {
            if (__result == -1 && __instance == Main.instance.language)
                __result = 0;
        }
    }
    [HarmonyPatch(typeof(BatteryCharger))]
    static class Patch_BatteryChargerCheck
    {
        [HarmonyPatch("DoesRequireCharge")]
        [HarmonyPrefix]
        static bool DoesRequireCharge(Battery battery, ref bool __result)
        {
            if (battery.GetPower(out var cur, out var max))
            {
                __result = cur < max;
                return false;
            }
            return true;
        }
        [HarmonyPatch("AttemptRechargeBattery")]
        [HarmonyPrefix]
        static bool AttemptRechargeBattery(Battery battery, ref bool __result)
        {
            if (battery.GetPower(out var cur, out var max))
            {
                __result = cur < max;
                return __result;
            }
            return true;
        }
    }
}