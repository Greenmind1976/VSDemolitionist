using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using System;
using System.Text;
using Vintagestory.API.Client;

namespace VSDemolitionist;

public class ItemDetonator : Item
{
    private const string DetonatorAttrRoot = "detonator";
    private const float DefaultDetonationRadius = 24f;
    private const float DefaultActivationTime = 1.0f;
    private const string AttrLit = "vsd_lit";
    private const string AttrBombTier = "vsd_bombTier";
    private const string BlastingChargeTier = "blasting-charge";
    private ItemSlot? guiIconSlot;

    private float GetDetonatorFloat(ItemStack? stack, string key, float defaultValue)
    {
        if (key == "radius")
        {
            return VSDemolitionistModSystem.GetDetonatorRadius();
        }

        return stack?.Collectible?.Attributes?[DetonatorAttrRoot]?[key].AsFloat(defaultValue) ?? defaultValue;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        ItemStack? stack = inSlot?.Itemstack;
        if (stack == null) return;

        float radius = GetDetonatorFloat(stack, "radius", DefaultDetonationRadius);
        dsc.AppendLine();
        dsc.AppendLine($"Triggers nearby blasting charges within {Math.Round(radius):0} blocks.");
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);

        if (target != EnumItemRenderTarget.Gui || VSDemolitionistModSystem.Use3DIcons())
        {
            return;
        }

        Item? iconItem = capi.World.GetItem(new AssetLocation("vsdemolitionist", "detonatoricon-2d"));
        if (iconItem == null)
        {
            return;
        }

        guiIconSlot ??= new DummySlot();
        guiIconSlot.Itemstack = new ItemStack(iconItem, 1);
        renderinfo = capi.Render.GetItemStackRenderInfo(guiIconSlot, target, 0);
    }

    public override void OnHeldInteractStart(
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel,
        bool firstEvent,
        ref EnumHandHandling handling)
    {
        if (!firstEvent || slot?.Itemstack == null)
        {
            return;
        }

        handling = EnumHandHandling.PreventDefault;
    }

    public override bool OnHeldInteractStep(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (slot?.Itemstack == null) return false;
        float activationTime = GetDetonatorFloat(slot.Itemstack, "activationTime", DefaultActivationTime);
        return secondsUsed < activationTime;
    }

    public override void OnHeldInteractStop(
        float secondsUsed,
        ItemSlot slot,
        EntityAgent byEntity,
        BlockSelection blockSel,
        EntitySelection entitySel)
    {
        if (slot?.Itemstack == null) return;
        float activationTime = GetDetonatorFloat(slot.Itemstack, "activationTime", DefaultActivationTime);
        if (secondsUsed < activationTime) return;

        TriggerDetonation(slot, byEntity);
    }

    private void TriggerDetonation(ItemSlot slot, EntityAgent byEntity)
    {
        if (byEntity.World.Side != EnumAppSide.Server)
        {
            return;
        }

        if (byEntity is not EntityPlayer entityPlayer || entityPlayer.Player is not IServerPlayer serverPlayer)
        {
            return;
        }

        ICoreServerAPI sapi = (ICoreServerAPI)byEntity.World.Api;
        float radius = GetDetonatorFloat(slot.Itemstack, "radius", DefaultDetonationRadius);
        double radiusSq = radius * radius;
        int triggered = 0;
        int foundCharges = 0;

        Entity[] entities = new Entity[sapi.World.LoadedEntities.Count];
        sapi.World.LoadedEntities.Values.CopyTo(entities, 0);
        foreach (Entity entity in entities)
        {
            if (entity is not EntityBomb bomb) continue;

            bool isCharge =
                bomb.WatchedAttributes.GetString(AttrBombTier, "") == BlastingChargeTier ||
                string.Equals(bomb.Code?.Path, "bombstuck-charge", StringComparison.OrdinalIgnoreCase);

            if (!isCharge) continue;

            double dx = bomb.Pos.X - byEntity.Pos.X;
            double dy = bomb.Pos.Y - byEntity.Pos.Y;
            double dz = bomb.Pos.Z - byEntity.Pos.Z;
            if ((dx * dx + dy * dy + dz * dz) > radiusSq) continue;

            foundCharges++;
            if (bomb.WatchedAttributes.GetBool(AttrLit, false)) continue;
            bomb.SetThrower(byEntity);
            bomb.StartFuse();
            triggered++;
        }

        serverPlayer.SendMessage(
            GlobalConstants.GeneralChatGroup,
            triggered > 0
                ? $"Detonator triggered {triggered} blasting charge(s)."
                : foundCharges > 0
                    ? "All nearby blasting charges are already lit."
                    : "No blasting charges in range.",
            EnumChatType.Notification
        );
    }
}
