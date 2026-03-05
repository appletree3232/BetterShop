using System;
using System.Collections;
using System.Collections.Generic;
using Dungeonator;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

// Token: 0x02001548 RID: 5448
public class BaseShopController : DungeonPlaceableBehaviour, IPlaceConfigurable
{
    // Token: 0x17001260 RID: 4704
    // (get) Token: 0x06007CA5 RID: 31909 RVA: 0x003232B4 File Offset: 0x003214B4
    protected bool IsMainShopkeep
    {
        get
        {
            return this.cat;
        }
    }

    // Token: 0x17001261 RID: 4705
    // (get) Token: 0x06007CA6 RID: 31910 RVA: 0x003232C4 File Offset: 0x003214C4
    // (set) Token: 0x06007CA7 RID: 31911 RVA: 0x003232E4 File Offset: 0x003214E4
    public float StealChance
    {
        get
        {
            return (!this.IsMainShopkeep) ? this.m_stealChance : BaseShopController.s_mainShopkeepStealChance;
        }
        protected set
        {
            if (this.IsMainShopkeep)
            {
                BaseShopController.s_mainShopkeepStealChance = value;
            }
            else
            {
                this.m_stealChance = value;
            }
        }
    }

    // Token: 0x17001262 RID: 4706
    // (get) Token: 0x06007CA8 RID: 31912 RVA: 0x00323304 File Offset: 0x00321504
    // (set) Token: 0x06007CA9 RID: 31913 RVA: 0x00323324 File Offset: 0x00321524
    public int ItemsStolen
    {
        get
        {
            return (!this.IsMainShopkeep) ? this.m_itemsStolen : BaseShopController.s_mainShopkeepItemsStolen;
        }
        protected set
        {
            if (this.IsMainShopkeep)
            {
                BaseShopController.s_mainShopkeepItemsStolen = value;
            }
            else
            {
                this.m_itemsStolen = value;
            }
        }
    }

    // Token: 0x17001263 RID: 4707
    // (get) Token: 0x06007CAA RID: 31914 RVA: 0x00323344 File Offset: 0x00321544
    public Vector2 CenterPosition
    {
        get
        {
            if (base.specRigidbody && base.specRigidbody.HitboxPixelCollider != null)
            {
                return base.specRigidbody.HitboxPixelCollider.UnitCenter;
            }
            if (base.sprite)
            {
                return base.sprite.WorldCenter;
            }
            return base.transform.position.XY();
        }
    }

    // Token: 0x17001264 RID: 4708
    // (get) Token: 0x06007CAB RID: 31915 RVA: 0x003233B0 File Offset: 0x003215B0
    public bool IsCapableOfBeingStolenFrom
    {
        get
        {
            return this.m_capableOfBeingStolenFrom.Value;
        }
    }

    // Token: 0x06007CAC RID: 31916 RVA: 0x003233C0 File Offset: 0x003215C0
    public void SetCapableOfBeingStolenFrom(bool value, string reason, float? duration = null)
    {
        this.m_capableOfBeingStolenFrom.SetOverride(reason, value, duration);
        for (int i = 0; i < GameManager.Instance.AllPlayers.Length; i++)
        {
            GameManager.Instance.AllPlayers[i].ForceRefreshInteractable = true;
        }
    }

    // Token: 0x17001265 RID: 4709
    // (get) Token: 0x06007CAD RID: 31917 RVA: 0x0032340C File Offset: 0x0032160C
    public bool WasCaughtStealing
    {
        get
        {
            return this.m_wasCaughtStealing;
        }
    }

    // Token: 0x06007CAE RID: 31918 RVA: 0x00323414 File Offset: 0x00321614
    protected IEnumerator Start()
    {
        StaticReferenceManager.AllShops.Add(this);
        if (this.IsMainShopkeep)
        {
            this.StealChance = Mathf.Min(this.StealChance + 0.25f, 1f);
        }
        if (this.baseShopType == BaseShopController.AdditionalShopType.FOYER_META)
        {
            base.StartCoroutine(this.HandleDelayedFoyerInitialization());
        }
        else
        {
            this.DoSetup();
        }
        this.m_shopkeep = this.shopkeepFSM.GetComponent<TalkDoerLite>();
        if (this.m_shopkeep)
        {
            this.m_fakeEffectHandler = this.m_shopkeep.gameObject.GetOrAddComponent<FakeGameActorEffectHandler>();
        }
        if (this.baseShopType != BaseShopController.AdditionalShopType.FOYER_META)
        {
            yield return null;
            tk2dBaseSprite[] childSprites = base.GetComponentsInChildren<tk2dBaseSprite>(true);
            for (int i = 0; i < childSprites.Length; i++)
            {
                childSprites[i].UpdateZDepth();
            }
        }
        if (this.IsMainShopkeep && BaseShopController.s_emptyFutureShops)
        {
            this.State = BaseShopController.ShopState.Gone;
        }
        while (Dungeon.IsGenerating)
        {
            yield return null;
        }
        Debug.LogWarning("s_empty?: " + BaseShopController.s_emptyFutureShops);
        if (this.IsMainShopkeep && BaseShopController.s_emptyFutureShops)
        {
            this.State = BaseShopController.ShopState.Gone;
        }
        if (this.IsBeetleMerchant)
        {
            tk2dSpriteAnimator component = base.transform.Find("dung").GetComponent<tk2dSpriteAnimator>();
            if (global::UnityEngine.Random.value > 0.5f)
            {
                this.m_shopkeep.transform.position += new Vector3(0.125f, 1.9375f, 0f);
                this.m_shopkeep.sprite.HeightOffGround = 1f;
                this.m_shopkeep.sprite.UpdateZDepth();
                AIAnimator aiAnimator = this.m_shopkeep.aiAnimator;
                aiAnimator.IdleAnimation.Prefix = "idle_sit";
                aiAnimator.TalkAnimation.Type = DirectionalAnimation.DirectionType.Single;
                aiAnimator.TalkAnimation.Prefix = "talk_sit";
                aiAnimator.OtherAnimations[0].anim.AnimNames[0] = "not_sit_right";
                aiAnimator.OtherAnimations[0].anim.AnimNames[1] = "no_sit_left";
                aiAnimator.OtherAnimations[1].anim.Type = DirectionalAnimation.DirectionType.Single;
                aiAnimator.OtherAnimations[1].anim.Prefix = "yes_sit";
                if (component)
                {
                    component.sprite.SetSprite("dung_pack_sit_001");
                }
            }
            else if (component)
            {
                component.Play("dungpack_idle");
            }
        }
        yield break;
    }

    // Token: 0x06007CAF RID: 31919 RVA: 0x00323430 File Offset: 0x00321630
    private IEnumerator HandleDelayedFoyerInitialization()
    {
        while (GameManager.Instance.IsSelectingCharacter || GameManager.Instance.PrimaryPlayer == null)
        {
            yield return null;
        }
        this.DoSetup();
        yield break;
    }

    // Token: 0x06007CB0 RID: 31920 RVA: 0x0032344C File Offset: 0x0032164C
    private void Update()
    {
        if ((this.State == BaseShopController.ShopState.Default || this.State == BaseShopController.ShopState.GunDrawn) && SuperReaperController.Instance != null)
        {
            IntVector2 intVector = SuperReaperController.Instance.sprite.WorldCenter.ToIntVector2(VectorConversions.Floor);
            if (GameManager.Instance.Dungeon.data.CheckInBounds(intVector))
            {
                CellData cellData = GameManager.Instance.Dungeon.data[intVector];
                if (cellData != null && cellData.parentRoom == this.m_room)
                {
                    this.PreventTeleportingPlayerAway = true;
                    this.State = BaseShopController.ShopState.TeleportAway;
                }
            }
        }
        if (this.baseShopType == BaseShopController.AdditionalShopType.FOYER_META && this.m_itemControllers != null && this.IsBeetleMerchant && !this.BeetleExhausted)
        {
            bool flag = true;
            for (int i = 0; i < this.m_itemControllers.Count; i++)
            {
                if (!this.m_itemControllers[i].Acquired)
                {
                    flag = false;
                }
            }
            if (flag)
            {
                this.m_shopkeep.ShopStockStatus = ((!this.m_onLastStockBeetle) ? Tribool.Ready : Tribool.Complete);
                if (this.m_onLastStockBeetle)
                {
                    GameStatsManager.Instance.SetFlag(GungeonFlags.BLUEPRINTBEETLE_GOLDIES, true);
                }
                this.BeetleExhausted = true;
                GameStatsManager.Instance.SetFlag(GungeonFlags.SHOP_BEETLE_ACTIVE, false);
                GameStatsManager.Instance.AccumulatedBeetleMerchantChance = 0f;
                GameStatsManager.Instance.AccumulatedUsedBeetleMerchantChance = 0f;
            }
        }
        this.m_timeSinceLastHit += BraveTime.DeltaTime;
        if (this.State == BaseShopController.ShopState.Default || this.State == BaseShopController.ShopState.GunDrawn)
        {
            if (this.IsMainShopkeep)
            {
                for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
                {
                    PlayerController playerController = GameManager.Instance.AllPlayers[j];
                    if (playerController && playerController.healthHaver.IsAlive && playerController.CurrentRoom == this.m_room && playerController.IsFiring)
                    {
                        this.PlayerFired();
                    }
                }
            }
        }
        else if (this.State == BaseShopController.ShopState.PreTeleportAway)
        {
            if (this.m_shopkeep.IsTalking)
            {
                EndConversation.ForceEndConversation(this.m_shopkeep);
            }
            this.m_preTeleportTimer += BraveTime.DeltaTime;
            if (this.m_preTeleportTimer > 2f)
            {
                this.State = BaseShopController.ShopState.TeleportAway;
            }
        }
        else if (this.State == BaseShopController.ShopState.TeleportAway)
        {
            if (this.m_shopkeep.IsTalking)
            {
                EndConversation.ForceEndConversation(this.m_shopkeep);
            }
            if (!this.m_shopkeep.aiAnimator.IsPlaying("button"))
            {
                foreach (PlayerController playerController2 in GameManager.Instance.AllPlayers)
                {
                    if (playerController2)
                    {
                        if (playerController2.CurrentRoom != null && playerController2.CurrentRoom != this.m_room && playerController2.CurrentRoom.IsSealed)
                        {
                            this.PreventTeleportingPlayerAway = true;
                        }
                    }
                }
                if (!this.PreventTeleportingPlayerAway)
                {
                    PlayerController bestActivePlayer = GameManager.Instance.BestActivePlayer;
                    if (bestActivePlayer.CurrentRoom == this.m_room)
                    {
                        bestActivePlayer.EscapeRoom(PlayerController.EscapeSealedRoomStyle.TELEPORTER, false, null);
                        AkSoundEngine.PostEvent("Play_OBJ_teleport_depart_01", bestActivePlayer.gameObject);
                    }
                    if (GameManager.Instance.CurrentGameType == GameManager.GameType.COOP_2_PLAYER)
                    {
                        PlayerController otherPlayer = GameManager.Instance.GetOtherPlayer(bestActivePlayer);
                        if (otherPlayer && otherPlayer.healthHaver.IsAlive)
                        {
                            otherPlayer.ReuniteWithOtherPlayer(bestActivePlayer, false);
                        }
                    }
                }
                this.State = BaseShopController.ShopState.Gone;
            }
        }
        else if (this.State == BaseShopController.ShopState.Hostile)
        {
            bool flag2 = false;
            for (int l = 0; l < GameManager.Instance.AllPlayers.Length; l++)
            {
                PlayerController playerController3 = GameManager.Instance.AllPlayers[l];
                if (playerController3 && playerController3.healthHaver.IsAlive && playerController3.CurrentRoom == this.m_room)
                {
                    flag2 = true;
                }
            }
            if (!flag2)
            {
                this.State = BaseShopController.ShopState.Gone;
                return;
            }
            this.m_preTeleportTimer += BraveTime.DeltaTime;
            if (this.m_preTeleportTimer > 10f)
            {
                this.State = BaseShopController.ShopState.TeleportAway;
            }
        }
        else if (this.State == BaseShopController.ShopState.RefuseService)
        {
            bool flag3 = false;
            for (int m = 0; m < GameManager.Instance.AllPlayers.Length; m++)
            {
                PlayerController playerController4 = GameManager.Instance.AllPlayers[m];
                if (playerController4 && playerController4.healthHaver.IsAlive && playerController4.CurrentRoom == this.m_room)
                {
                    flag3 = true;
                }
            }
            if (!flag3)
            {
                this.State = BaseShopController.ShopState.Gone;
            }
        }
        bool flag4 = this.m_capableOfBeingStolenFrom.UpdateTimers(BraveTime.DeltaTime);
        if (flag4)
        {
            for (int n = 0; n < GameManager.Instance.AllPlayers.Length; n++)
            {
                GameManager.Instance.AllPlayers[n].ForceRefreshInteractable = true;
            }
        }
    }

    // Token: 0x06007CB1 RID: 31921 RVA: 0x0032398C File Offset: 0x00321B8C
    protected override void OnDestroy()
    {
        if (this.m_shopkeep && this.m_shopkeep.bulletBank)
        {
            AIBulletBank bulletBank = this.m_shopkeep.bulletBank;
            bulletBank.OnProjectileCreated = (Action<Projectile>)Delegate.Remove(bulletBank.OnProjectileCreated, new Action<Projectile>(this.OnProjectileCreated));
        }
        StaticReferenceManager.AllShops.Remove(this);
        base.OnDestroy();
    }

    // Token: 0x06007CB2 RID: 31922 RVA: 0x003239FC File Offset: 0x00321BFC
    public virtual void NotifyFailedPurchase(ShopItemController itemController)
    {
        if (this.shopkeepFSM != null)
        {
            FsmObject fsmObject = this.shopkeepFSM.FsmVariables.FindFsmObject("referencedItem");
            if (fsmObject != null)
            {
                fsmObject.Value = itemController;
            }
            this.shopkeepFSM.SendEvent("failedPurchase");
        }
    }

    // Token: 0x06007CB3 RID: 31923 RVA: 0x00323A50 File Offset: 0x00321C50
    public virtual void PurchaseItem(ShopItemController item, bool actualPurchase = true, bool allowSign = true)
    {
        float num = -1f;
        if (item && item.sprite)
        {
            num = item.sprite.HeightOffGround;
        }
        if (actualPurchase)
        {
            if (this.baseShopType == BaseShopController.AdditionalShopType.TRUCK)
            {
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MERCHANT_PURCHASES_TRUCK, 1f);
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MONEY_SPENT_AT_TRUCK_SHOP, (float)item.ModifiedPrice);
            }
            if (this.baseShopType == BaseShopController.AdditionalShopType.GOOP)
            {
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MERCHANT_PURCHASES_GOOP, 1f);
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MONEY_SPENT_AT_GOOP_SHOP, (float)item.ModifiedPrice);
            }
            if (this.baseShopType == BaseShopController.AdditionalShopType.CURSE)
            {
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MERCHANT_PURCHASES_CURSE, 1f);
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MONEY_SPENT_AT_CURSE_SHOP, (float)item.ModifiedPrice);
                StatModifier statModifier = new StatModifier();
                statModifier.amount = 2.5f;
                statModifier.modifyType = StatModifier.ModifyMethod.ADDITIVE;
                statModifier.statToBoost = PlayerStats.StatType.Curse;
                item.LastInteractingPlayer.ownerlessStatModifiers.Add(statModifier);
                item.LastInteractingPlayer.stats.RecalculateStats(item.LastInteractingPlayer, false, false);
            }
            if (this.baseShopType == BaseShopController.AdditionalShopType.BLANK)
            {
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MERCHANT_PURCHASES_BLANK, 1f);
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MONEY_SPENT_AT_BLANK_SHOP, (float)item.ModifiedPrice);
            }
            if (this.baseShopType == BaseShopController.AdditionalShopType.KEY)
            {
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MERCHANT_PURCHASES_KEY, 1f);
                GameStatsManager.Instance.RegisterStatChange(TrackedStats.MONEY_SPENT_AT_KEY_SHOP, (float)item.ModifiedPrice);
            }
            if (this.shopkeepFSM != null && this.baseShopType != BaseShopController.AdditionalShopType.RESRAT_SHORTCUT)
            {
                FsmObject fsmObject = this.shopkeepFSM.FsmVariables.FindFsmObject("referencedItem");
                if (fsmObject != null)
                {
                    fsmObject.Value = item;
                }
                this.shopkeepFSM.SendEvent("succeedPurchase");
            }
        }
        if (!item.item.PersistsOnPurchase)
        {
            if (allowSign)
            {
                GameObject gameObject = (GameObject)global::UnityEngine.Object.Instantiate(BraveResources.Load("Global Prefabs/Sign_SoldOut", ".prefab"));
                tk2dBaseSprite component = gameObject.GetComponent<tk2dBaseSprite>();
                component.PlaceAtPositionByAnchor(item.sprite.WorldCenter, tk2dBaseSprite.Anchor.MiddleCenter);
                gameObject.transform.position = gameObject.transform.position.Quantize(0.0625f);
                component.HeightOffGround = num;
                component.UpdateZDepth();
            }
            GameObject gameObject2 = (GameObject)global::UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
            tk2dBaseSprite component2 = gameObject2.GetComponent<tk2dBaseSprite>();
            component2.PlaceAtPositionByAnchor(item.sprite.WorldCenter.ToVector3ZUp(0f), tk2dBaseSprite.Anchor.MiddleCenter);
            component2.transform.position = component2.transform.position.Quantize(0.0625f);
            component2.HeightOffGround = 5f;
            component2.UpdateZDepth();
            this.m_room.DeregisterInteractable(item);
            global::UnityEngine.Object.Destroy(item.gameObject);
        }
        if (this.baseShopType == BaseShopController.AdditionalShopType.RESRAT_SHORTCUT)
        {
            this.m_numberThingsPurchased++;
            GlobalDungeonData.ValidTilesets tilesetId = GameManager.Instance.Dungeon.tileIndices.tilesetId;
            int num2;
            if (tilesetId != GlobalDungeonData.ValidTilesets.GUNGEON)
            {
                if (tilesetId != GlobalDungeonData.ValidTilesets.MINEGEON)
                {
                    if (tilesetId != GlobalDungeonData.ValidTilesets.CATACOMBGEON)
                    {
                        if (tilesetId != GlobalDungeonData.ValidTilesets.FORGEGEON)
                        {
                            num2 = 1;
                        }
                        else
                        {
                            num2 = 3;
                        }
                    }
                    else
                    {
                        num2 = 2;
                    }
                }
                else
                {
                    num2 = 1;
                }
            }
            else
            {
                num2 = 1;
            }
            if (this.m_numberThingsPurchased >= num2)
            {
                for (int i = 0; i < this.m_itemControllers.Count; i++)
                {
                    if (!this.m_itemControllers[i].Acquired)
                    {
                        this.m_itemControllers[i].ForceOutOfStock();
                    }
                }
                if (this.shopkeepFSM != null)
                {
                    FsmObject fsmObject2 = this.shopkeepFSM.FsmVariables.FindFsmObject("referencedItem");
                    if (fsmObject2 != null)
                    {
                        fsmObject2.Value = item;
                    }
                    this.shopkeepFSM.SendEvent("succeedPurchase");
                    this.m_shopkeep.IsInteractable = false;
                }
            }
        }
    }

    // Token: 0x06007CB4 RID: 31924 RVA: 0x00323E4C File Offset: 0x0032204C
    public void NotifyStealSucceeded()
    {
        this.ItemsStolen++;
        if (this.IsMainShopkeep)
        {
            this.StealChance = ((this.ItemsStolen > 1) ? 0.1f : 0.5f);
        }
        else
        {
            this.StealChance = 0.1f;
        }
    }

    // Token: 0x06007CB5 RID: 31925 RVA: 0x00323EA4 File Offset: 0x003220A4
    public void NotifyStealFailed()
    {
        this.shopkeepFSM.SendEvent("caughtStealing");
        this.m_capableOfBeingStolenFrom.ClearOverrides();
        if (this.m_fakeEffectHandler)
        {
            this.m_fakeEffectHandler.RemoveAllEffects(false);
        }
        if (this.IsMainShopkeep)
        {
            this.State = BaseShopController.ShopState.PreTeleportAway;
        }
        else
        {
            this.State = BaseShopController.ShopState.RefuseService;
        }
        this.m_wasCaughtStealing = true;
    }

    // Token: 0x06007CB6 RID: 31926 RVA: 0x00323F10 File Offset: 0x00322110
    public bool AttemptToSteal()
    {
        return global::UnityEngine.Random.value <= this.StealChance;
    }

    // Token: 0x17001266 RID: 4710
    // (get) Token: 0x06007CB7 RID: 31927 RVA: 0x00323F24 File Offset: 0x00322124
    // (set) Token: 0x06007CB8 RID: 31928 RVA: 0x00323F2C File Offset: 0x0032212C
    public static bool HasLockedShopProcedurally
    {
        get
        {
            return BaseShopController.m_hasLockedShopProcedurally;
        }
        set
        {
            BaseShopController.m_hasLockedShopProcedurally = value;
        }
    }

    // Token: 0x06007CB9 RID: 31929 RVA: 0x00323F34 File Offset: 0x00322134
    public virtual void ConfigureOnPlacement(RoomHandler room)
    {
        if (this.baseShopType != BaseShopController.AdditionalShopType.RESRAT_SHORTCUT)
        {
            room.IsShop = true;
        }
        this.m_room = room;
        if (this.OptionalMinimapIcon != null)
        {
            Minimap.Instance.RegisterRoomIcon(this.m_room, this.OptionalMinimapIcon, false);
        }
        room.Entered += this.HandleEnter;
        bool isSeeded = GameManager.Instance.IsSeeded;
        if (GameManager.Instance.Dungeon.tileIndices.tilesetId == GlobalDungeonData.ValidTilesets.CASTLEGEON || (this.baseShopType != BaseShopController.AdditionalShopType.BLANK && this.baseShopType != BaseShopController.AdditionalShopType.CURSE && this.baseShopType != BaseShopController.AdditionalShopType.GOOP && this.baseShopType != BaseShopController.AdditionalShopType.TRUCK) || room.connectedRooms.Count == 1)
        {
        }
    }

    // Token: 0x06007CBA RID: 31930 RVA: 0x00323FFC File Offset: 0x003221FC
    private PickupObject GetRandomLockedPaydayItem()
    {
        GenericLootTable itemsLootTable = GameManager.Instance.RewardManager.ItemsLootTable;
        List<PickupObject> list = new List<PickupObject>();
        for (int i = 0; i < itemsLootTable.defaultItemDrops.elements.Count; i++)
        {
            WeightedGameObject weightedGameObject = itemsLootTable.defaultItemDrops.elements[i];
            if (weightedGameObject.gameObject)
            {
                PickupObject component = weightedGameObject.gameObject.GetComponent<PickupObject>();
                if (component && (component is BankMaskItem || component is BankBagItem || component is PaydayDrillItem))
                {
                    EncounterTrackable encounterTrackable = component.encounterTrackable;
                    if (encounterTrackable && !encounterTrackable.PrerequisitesMet())
                    {
                        list.Add(component);
                    }
                }
            }
        }
        if (list.Count <= 0)
        {
            return null;
        }
        return list[global::UnityEngine.Random.Range(0, list.Count)];
    }

    // Token: 0x06007CBB RID: 31931 RVA: 0x003240E8 File Offset: 0x003222E8
    private void HandleEnter(PlayerController p)
    {
        if (!this.m_hasBeenEntered && this.baseShopType == BaseShopController.AdditionalShopType.NONE)
        {
            GlobalDungeonData.ValidTilesets tilesetId = GameManager.Instance.Dungeon.tileIndices.tilesetId;
            this.ReinitializeFirstItemToKey();
        }
        this.m_hasBeenEntered = true;
        if (this.FlagToSetOnEncounter != GungeonFlags.NONE)
        {
            GameStatsManager.Instance.SetFlag(this.FlagToSetOnEncounter, true);
        }
    }

    // Token: 0x06007CBC RID: 31932 RVA: 0x0032414C File Offset: 0x0032234C
    private void OnProjectileCreated(Projectile projectile)
    {
        projectile.OwnerName = StringTableManager.GetEnemiesString("#JUSTICE_ENCNAME", -1);
    }

    // Token: 0x17001267 RID: 4711
    // (get) Token: 0x06007CBD RID: 31933 RVA: 0x00324160 File Offset: 0x00322360
    // (set) Token: 0x06007CBE RID: 31934 RVA: 0x00324168 File Offset: 0x00322368
    private BaseShopController.ShopState State
    {
        get
        {
            return this.m_state;
        }
        set
        {
            this.EndState(this.m_state);
            this.m_state = value;
            this.BeginState(this.m_state);
        }
    }

    // Token: 0x06007CBF RID: 31935 RVA: 0x0032418C File Offset: 0x0032238C
    private void BeginState(BaseShopController.ShopState state)
    {
        if (state == BaseShopController.ShopState.GunDrawn)
        {
            for (int i = 0; i < this.m_itemControllers.Count; i++)
            {
                if (this.m_itemControllers[i])
                {
                    this.m_itemControllers[i].CurrentPrice *= 2;
                }
            }
            for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
            {
                PlayerController playerController = GameManager.Instance.AllPlayers[j];
                if (playerController && playerController.healthHaver.IsAlive)
                {
                    playerController.ForceRefreshInteractable = true;
                }
            }
        }
        else if (state == BaseShopController.ShopState.Hostile)
        {
            this.shopkeepFSM.enabled = false;
            this.m_shopkeep.IsInteractable = false;
            this.LockItems();
            BaseShopController.s_emptyFutureShops = true;
            this.m_shopkeep.behaviorSpeculator.enabled = true;
            AIBulletBank bulletBank = this.m_shopkeep.bulletBank;
            bulletBank.OnProjectileCreated = (Action<Projectile>)Delegate.Combine(bulletBank.OnProjectileCreated, new Action<Projectile>(this.OnProjectileCreated));
        }
        else if (state == BaseShopController.ShopState.PreTeleportAway)
        {
            this.m_preTeleportTimer = 0f;
            this.m_shopkeep.IsInteractable = false;
            this.LockItems();
            BaseShopController.s_emptyFutureShops = true;
        }
        else if (state == BaseShopController.ShopState.TeleportAway)
        {
            if (this.IsMainShopkeep)
            {
                this.shopkeepFSM.enabled = false;
                this.m_shopkeep.IsInteractable = false;
                this.m_shopkeep.behaviorSpeculator.InterruptAndDisable();
            }
            this.m_shopkeep.aiAnimator.PlayUntilCancelled("button", false, null, -1f, false);
            SpriteOutlineManager.RemoveOutlineFromSprite(this.m_shopkeep.sprite, false);
            this.m_shopkeep.sprite.HeightOffGround = 0f;
            this.m_shopkeep.sprite.UpdateZDepth();
        }
        else if (state == BaseShopController.ShopState.Gone)
        {
            if (this.IsMainShopkeep)
            {
                this.shopkeepFSM.enabled = false;
                this.m_shopkeep.IsInteractable = false;
                this.m_shopkeep.behaviorSpeculator.InterruptAndDisable();
                if (this.m_shopkeep.spriteAnimator.CurrentClip.name != "button_hit")
                {
                    this.m_shopkeep.aiAnimator.PlayUntilCancelled("hide", false, null, -1f, false);
                }
                this.m_shopkeep.specRigidbody.enabled = false;
                global::UnityEngine.Object.Destroy(this.m_shopkeep.ultraFortunesFavor);
                this.m_shopkeep.RegenerateCache();
                if (this.cat)
                {
                    tk2dBaseSprite component = this.cat.GetComponent<tk2dBaseSprite>();
                    GameObject gameObject = (GameObject)global::UnityEngine.Object.Instantiate(ResourceCache.Acquire("Global VFX/VFX_Item_Spawn_Poof"));
                    tk2dBaseSprite component2 = gameObject.GetComponent<tk2dBaseSprite>();
                    component2.PlaceAtPositionByAnchor(component.WorldCenter.ToVector3ZUp(0f), tk2dBaseSprite.Anchor.MiddleCenter);
                    component2.transform.position = component2.transform.position.Quantize(0.0625f);
                    component2.HeightOffGround = 10f;
                    component2.UpdateZDepth();
                    this.cat.SetActive(false);
                }
            }
            for (int k = 0; k < this.m_itemControllers.Count; k++)
            {
                if (this.m_itemControllers[k])
                {
                    ShopItemController shopItemController = this.m_itemControllers[k];
                    bool flag = false;
                    if (shopItemController.item is BankMaskItem || shopItemController.item is BankBagItem || shopItemController.item is PaydayDrillItem)
                    {
                        EncounterTrackable encounterTrackable = shopItemController.item.encounterTrackable;
                        if (encounterTrackable && !encounterTrackable.PrerequisitesMet())
                        {
                            flag = true;
                            shopItemController.Locked = false;
                            shopItemController.OverridePrice = new int?(0);
                            if (shopItemController.SetsFlagOnSteal)
                            {
                                GameStatsManager.Instance.SetFlag(shopItemController.FlagToSetOnSteal, true);
                            }
                        }
                    }
                    if (!flag)
                    {
                        this.m_itemControllers[k].ForceOutOfStock();
                    }
                }
            }
        }
        else if (state == BaseShopController.ShopState.RefuseService)
        {
            this.LockItems();
            this.m_shopkeep.SuppressRoomEnterExitEvents = true;
        }
    }

    // Token: 0x06007CC0 RID: 31936 RVA: 0x003245B8 File Offset: 0x003227B8
    private void EndState(BaseShopController.ShopState state)
    {
    }

    // Token: 0x06007CC1 RID: 31937 RVA: 0x003245BC File Offset: 0x003227BC
    private void PlayerFired()
    {
        if (this.m_timeSinceLastHit > 2f)
        {
            this.m_hitCount++;
            this.m_timeSinceLastHit = 0f;
            if (this.m_state == BaseShopController.ShopState.Default)
            {
                if (this.m_hitCount <= 1)
                {
                    this.shopkeepFSM.SendEvent("betrayalWarning");
                }
                else
                {
                    this.shopkeepFSM.SendEvent("betrayal");
                    this.State = BaseShopController.ShopState.GunDrawn;
                }
            }
            else if (this.m_state == BaseShopController.ShopState.GunDrawn)
            {
                this.State = BaseShopController.ShopState.Hostile;
            }
        }
    }

    // Token: 0x06007CC2 RID: 31938 RVA: 0x00324650 File Offset: 0x00322850
    public void ReinitializeFirstItemToKey()
    {
        if (this.baseShopType == BaseShopController.AdditionalShopType.NONE)
        {
            for (int i = 0; i < this.m_itemControllers.Count; i++)
            {
                if (this.m_itemControllers[i] && this.m_itemControllers[i].item && this.m_itemControllers[i].item.GetComponent<KeyBulletPickup>())
                {
                    return;
                }
            }
            int num = global::UnityEngine.Random.Range(0, this.m_numberOfFirstTypeItems);
            if (num < 0)
            {
                num = 0;
            }
            if (num >= this.m_shopItems.Count || num >= this.m_itemControllers.Count || !this.m_shopItems[num] || !this.m_itemControllers[num])
            {
                num = 0;
            }
            if (this.m_shopItems[num] && this.m_itemControllers[num])
            {
                GameObject gameObject = null;
                for (int j = 0; j < this.shopItems.defaultItemDrops.elements.Count; j++)
                {
                    if (this.shopItems.defaultItemDrops.elements[j].gameObject && this.shopItems.defaultItemDrops.elements[j].gameObject.GetComponent<KeyBulletPickup>())
                    {
                        gameObject = this.shopItems.defaultItemDrops.elements[j].gameObject;
                    }
                }
                this.m_shopItems[num] = gameObject;
                this.m_itemControllers[num].Initialize(gameObject.GetComponent<PickupObject>(), this);
            }
        }
    }

    // Token: 0x06007CC3 RID: 31939 RVA: 0x00324820 File Offset: 0x00322A20
    protected virtual void DoSetup()
    {
        this.m_shopItems = new List<GameObject>();
        List<int> list = new List<int>();
        Func<GameObject, float, float> func = null;
        if (SecretHandshakeItem.NumActive > 0)
        {
            func = delegate (GameObject prefabObject, float sourceWeight)
            {
                PickupObject component10 = prefabObject.GetComponent<PickupObject>();
                float num7 = sourceWeight;
                if (component10 != null)
                {
                    int quality = (int)component10.quality;
                    num7 *= 1f + (float)quality / 10f;
                }
                return num7;
            };
        }
        global::System.Random random = null;
        if (this.baseShopType == BaseShopController.AdditionalShopType.RESRAT_SHORTCUT)
        {
            if (GameStatsManager.Instance.CurrentResRatShopSeed < 0)
            {
                GameStatsManager.Instance.CurrentResRatShopSeed = global::UnityEngine.Random.Range(1, 1000000);
            }
            random = new global::System.Random(GameStatsManager.Instance.CurrentResRatShopSeed);
        }
        bool flag = GameStatsManager.Instance.IsRainbowRun && (this.baseShopType == BaseShopController.AdditionalShopType.BLANK || this.baseShopType == BaseShopController.AdditionalShopType.CURSE || this.baseShopType == BaseShopController.AdditionalShopType.GOOP || this.baseShopType == BaseShopController.AdditionalShopType.KEY || this.baseShopType == BaseShopController.AdditionalShopType.TRUCK);
        for (int i = 0; i < this.spawnPositions.Length; i++)
        {
            if (flag)
            {
                this.m_shopItems.Add(null);
            }
            else if (this.baseShopType == BaseShopController.AdditionalShopType.RESRAT_SHORTCUT)
            {
                GameObject shopItemResourcefulRatStyle = GameManager.Instance.RewardManager.GetShopItemResourcefulRatStyle(this.m_shopItems, random);
                this.m_shopItems.Add(shopItemResourcefulRatStyle);
            }
            else if (this.baseShopType == BaseShopController.AdditionalShopType.FOYER_META && this.ExampleBlueprintPrefab != null)
            {
                if (this.FoyerMetaShopForcedTiers)
                {
                    List<WeightedGameObject> compiledRawItems = this.shopItems.GetCompiledRawItems();
                    int num = 0;
                    bool flag2 = true;
                    while (flag2)
                    {
                        for (int j = num; j < num + this.spawnPositions.Length; j++)
                        {
                            if (j >= compiledRawItems.Count)
                            {
                                flag2 = false;
                                break;
                            }
                            GameObject gameObject = compiledRawItems[j].gameObject;
                            PickupObject component = gameObject.GetComponent<PickupObject>();
                            if (!component.encounterTrackable.PrerequisitesMet())
                            {
                                flag2 = false;
                                break;
                            }
                        }
                        if (flag2)
                        {
                            num += this.spawnPositions.Length;
                        }
                    }
                    if (num >= compiledRawItems.Count - this.spawnPositions.Length)
                    {
                        this.m_onLastStockBeetle = true;
                    }
                    for (int k = num; k < num + this.spawnPositions.Length; k++)
                    {
                        if (k >= compiledRawItems.Count)
                        {
                            this.m_shopItems.Add(null);
                            list.Add(1);
                        }
                        else
                        {
                            GameObject gameObject2 = compiledRawItems[k].gameObject;
                            PickupObject component2 = gameObject2.GetComponent<PickupObject>();
                            if (this.m_shopItems.Contains(gameObject2) || component2.encounterTrackable.PrerequisitesMet())
                            {
                                this.m_shopItems.Add(null);
                                list.Add(1);
                            }
                            else
                            {
                                this.m_shopItems.Add(gameObject2);
                                list.Add(Mathf.RoundToInt(compiledRawItems[k].weight));
                            }
                        }
                    }
                }
                else
                {
                    List<WeightedGameObject> compiledRawItems2 = this.shopItems.GetCompiledRawItems();
                    GameObject gameObject3 = null;
                    for (int l = 0; l < compiledRawItems2.Count; l++)
                    {
                        GameObject gameObject4 = compiledRawItems2[l].gameObject;
                        PickupObject component3 = gameObject4.GetComponent<PickupObject>();
                        if (!this.m_shopItems.Contains(gameObject4))
                        {
                            if (!component3.encounterTrackable.PrerequisitesMet())
                            {
                                gameObject3 = gameObject4;
                                list.Add(Mathf.RoundToInt(compiledRawItems2[l].weight));
                                break;
                            }
                        }
                    }
                    this.m_shopItems.Add(gameObject3);
                    if (gameObject3 == null)
                    {
                        list.Add(1);
                    }
                }
            }
            else
            {
                GameObject gameObject5 = this.shopItems.SubshopSelectByWeightWithoutDuplicatesFullPrereqs(this.m_shopItems, func, 1, GameManager.Instance.IsSeeded);
                this.m_shopItems.Add(gameObject5);
                if (gameObject5)
                {
                    this.m_numberOfFirstTypeItems++;
                }
            }
        }
        this.m_itemControllers = new List<ShopItemController>();
        for (int m = 0; m < this.spawnPositions.Length; m++)
        {
            Transform transform = this.spawnPositions[m];
            if (!flag && !(this.m_shopItems[m] == null))
            {
                PickupObject component4 = this.m_shopItems[m].GetComponent<PickupObject>();
                if (!(component4 == null))
                {
                    GameObject gameObject6 = new GameObject("Shop item " + m.ToString());
                    Transform transform2 = gameObject6.transform;
                    transform2.parent = transform;
                    transform2.localPosition = Vector3.zero;
                    EncounterTrackable component5 = component4.GetComponent<EncounterTrackable>();
                    if (component5 != null)
                    {
                        GameManager.Instance.ExtantShopTrackableGuids.Add(component5.EncounterGuid);
                    }
                    ShopItemController shopItemController = gameObject6.AddComponent<ShopItemController>();
                    this.AssignItemFacing(transform, shopItemController);
                    if (!this.m_room.IsRegistered(shopItemController))
                    {
                        this.m_room.RegisterInteractable(shopItemController);
                    }
                    if (this.baseShopType == BaseShopController.AdditionalShopType.FOYER_META && this.ExampleBlueprintPrefab != null)
                    {
                        GameObject gameObject7 = global::UnityEngine.Object.Instantiate<GameObject>(this.ExampleBlueprintPrefab, new Vector3(150f, -50f, -100f), Quaternion.identity);
                        ItemBlueprintItem component6 = gameObject7.GetComponent<ItemBlueprintItem>();
                        EncounterTrackable component7 = gameObject7.GetComponent<EncounterTrackable>();
                        component7.journalData.PrimaryDisplayName = component4.encounterTrackable.journalData.PrimaryDisplayName;
                        component7.journalData.NotificationPanelDescription = component4.encounterTrackable.journalData.NotificationPanelDescription;
                        component7.journalData.AmmonomiconFullEntry = component4.encounterTrackable.journalData.AmmonomiconFullEntry;
                        component7.journalData.AmmonomiconSprite = component4.encounterTrackable.journalData.AmmonomiconSprite;
                        component7.DoNotificationOnEncounter = false;
                        component6.UsesCustomCost = true;
                        component6.CustomCost = list[m];
                        GungeonFlags gungeonFlags = GungeonFlags.NONE;
                        for (int n = 0; n < component4.encounterTrackable.prerequisites.Length; n++)
                        {
                            if (component4.encounterTrackable.prerequisites[n].prerequisiteType == DungeonPrerequisite.PrerequisiteType.FLAG)
                            {
                                gungeonFlags = component4.encounterTrackable.prerequisites[n].saveFlagToCheck;
                            }
                        }
                        component6.SaveFlagToSetOnAcquisition = gungeonFlags;
                        component6.HologramIconSpriteName = component7.journalData.AmmonomiconSprite;
                        shopItemController.Initialize(component6, this);
                        gameObject7.SetActive(false);
                    }
                    else
                    {
                        shopItemController.Initialize(component4, this);
                    }
                    this.m_itemControllers.Add(shopItemController);
                }
            }
        }
        bool flag3 = false;
        if (this.shopItemsGroup2 != null && this.spawnPositionsGroup2.Length > 0)
        {
            int count = this.m_shopItems.Count;
            for (int num2 = 0; num2 < this.spawnPositionsGroup2.Length; num2++)
            {
                if (flag)
                {
                    this.m_shopItems.Add(null);
                }
                else
                {
                    float num3 = this.spawnGroupTwoItem1Chance;
                    if (num2 == 1)
                    {
                        num3 = this.spawnGroupTwoItem2Chance;
                    }
                    else if (num2 == 2)
                    {
                        num3 = this.spawnGroupTwoItem3Chance;
                    }
                    bool isSeeded = GameManager.Instance.IsSeeded;
                    if (((!isSeeded) ? global::UnityEngine.Random.value : BraveRandom.GenerationRandomValue()) < num3)
                    {
                        if (this.baseShopType == BaseShopController.AdditionalShopType.BLACKSMITH)
                        {
                            if (!GameStatsManager.Instance.IsRainbowRun)
                            {
                                if (((!isSeeded) ? global::UnityEngine.Random.value : BraveRandom.GenerationRandomValue()) > 0.5f)
                                {
                                    GameObject gameObject8 = this.shopItemsGroup2.SelectByWeightWithoutDuplicatesFullPrereqs(this.m_shopItems, true, GameManager.Instance.IsSeeded);
                                    this.m_shopItems.Add(gameObject8);
                                }
                                else
                                {
                                    GameObject rewardObjectShopStyle = GameManager.Instance.RewardManager.GetRewardObjectShopStyle(GameManager.Instance.PrimaryPlayer, true, false, this.m_shopItems);
                                    this.m_shopItems.Add(rewardObjectShopStyle);
                                }
                            }
                            else
                            {
                                this.m_shopItems.Add(null);
                            }
                        }
                        else
                        {
                            float replaceFirstRewardWithPickup = GameManager.Instance.RewardManager.CurrentRewardData.ReplaceFirstRewardWithPickup;
                            if (!flag3 && ((!isSeeded) ? global::UnityEngine.Random.value : BraveRandom.GenerationRandomValue()) < replaceFirstRewardWithPickup)
                            {
                                flag3 = true;
                                GameObject gameObject9 = this.shopItems.SelectByWeightWithoutDuplicatesFullPrereqs(this.m_shopItems, func, GameManager.Instance.IsSeeded);
                                this.m_shopItems.Add(gameObject9);
                            }
                            else if (!GameStatsManager.Instance.IsRainbowRun)
                            {
                                GameObject rewardObjectShopStyle2 = GameManager.Instance.RewardManager.GetRewardObjectShopStyle(GameManager.Instance.PrimaryPlayer, false, false, this.m_shopItems);
                                this.m_shopItems.Add(rewardObjectShopStyle2);
                            }
                            else
                            {
                                this.m_shopItems.Add(null);
                            }
                        }
                    }
                    else
                    {
                        this.m_shopItems.Add(null);
                    }
                }
            }
            bool flag4 = GameStatsManager.Instance.GetFlag(GungeonFlags.ACHIEVEMENT_BIGGEST_WALLET) || global::UnityEngine.Random.value < 0.05f;
            if (this.baseShopType == BaseShopController.AdditionalShopType.NONE && flag4 && !flag)
            {
                PickupObject randomLockedPaydayItem = this.GetRandomLockedPaydayItem();
                if (randomLockedPaydayItem)
                {
                    if (this.m_shopItems.Count - count < this.spawnPositionsGroup2.Length)
                    {
                        this.m_shopItems.Add(randomLockedPaydayItem.gameObject);
                    }
                    else
                    {
                        this.m_shopItems[global::UnityEngine.Random.Range(count, this.m_shopItems.Count)] = randomLockedPaydayItem.gameObject;
                    }
                }
            }
            for (int num4 = 0; num4 < this.spawnPositionsGroup2.Length; num4++)
            {
                Transform transform3 = this.spawnPositionsGroup2[num4];
                if (!flag && !(this.m_shopItems[count + num4] == null))
                {
                    PickupObject component8 = this.m_shopItems[count + num4].GetComponent<PickupObject>();
                    if (!(component8 == null))
                    {
                        GameObject gameObject10 = new GameObject("Shop 2 item " + num4.ToString());
                        Transform transform4 = gameObject10.transform;
                        transform4.parent = transform3;
                        transform4.localPosition = Vector3.zero;
                        EncounterTrackable component9 = component8.GetComponent<EncounterTrackable>();
                        if (component9 != null)
                        {
                            GameManager.Instance.ExtantShopTrackableGuids.Add(component9.EncounterGuid);
                        }
                        ShopItemController shopItemController2 = gameObject10.AddComponent<ShopItemController>();
                        this.AssignItemFacing(transform3, shopItemController2);
                        if (!this.m_room.IsRegistered(shopItemController2))
                        {
                            this.m_room.RegisterInteractable(shopItemController2);
                        }
                        shopItemController2.Initialize(component8, this);
                        this.m_itemControllers.Add(shopItemController2);
                    }
                }
            }
        }
        if (this.baseShopType == BaseShopController.AdditionalShopType.NONE || this.baseShopType == BaseShopController.AdditionalShopType.BLACKSMITH || this.baseShopType == BaseShopController.AdditionalShopType.FOYER_META)
        {
            List<ShopSubsidiaryZone> componentsInRoom = this.m_room.GetComponentsInRoom<ShopSubsidiaryZone>();
            for (int num5 = 0; num5 < componentsInRoom.Count; num5++)
            {
                componentsInRoom[num5].HandleSetup(this, this.m_room, this.m_shopItems, this.m_itemControllers);
            }
        }
        for (int num6 = 0; num6 < this.m_itemControllers.Count; num6++)
        {
            if (this.baseShopType == BaseShopController.AdditionalShopType.KEY)
            {
                this.m_itemControllers[num6].CurrencyType = ShopItemController.ShopCurrencyType.KEYS;
            }
            if (this.baseShopType == BaseShopController.AdditionalShopType.FOYER_META)
            {
                this.m_itemControllers[num6].CurrencyType = ShopItemController.ShopCurrencyType.META_CURRENCY;
            }
        }
    }

    // Token: 0x06007CC4 RID: 31940 RVA: 0x00325380 File Offset: 0x00323580
    private void AssignItemFacing(Transform spawnTransform, ShopItemController itemController)
    {
        if (this.baseShopType == BaseShopController.AdditionalShopType.FOYER_META)
        {
            itemController.UseOmnidirectionalItemFacing = true;
        }
        else if (spawnTransform.name.Contains("SIDE") || spawnTransform.name.Contains("EAST"))
        {
            itemController.itemFacing = DungeonData.Direction.EAST;
        }
        else if (spawnTransform.name.Contains("WEST"))
        {
            itemController.itemFacing = DungeonData.Direction.WEST;
        }
        else if (spawnTransform.name.Contains("NORTH"))
        {
            itemController.itemFacing = DungeonData.Direction.NORTH;
        }
    }

    // Token: 0x06007CC5 RID: 31941 RVA: 0x00325418 File Offset: 0x00323618
    private void LockItems()
    {
        for (int i = 0; i < this.m_itemControllers.Count; i++)
        {
            if (this.m_itemControllers[i])
            {
                this.m_itemControllers[i].Locked = true;
            }
        }
        for (int j = 0; j < GameManager.Instance.AllPlayers.Length; j++)
        {
            PlayerController playerController = GameManager.Instance.AllPlayers[j];
            if (playerController && playerController.healthHaver.IsAlive)
            {
                playerController.ForceRefreshInteractable = true;
            }
        }
    }

    // Token: 0x06007CC6 RID: 31942 RVA: 0x003254B8 File Offset: 0x003236B8
    public static MidGameStaticShopData GetStaticShopDataForMidGameSave()
    {
        return new MidGameStaticShopData
        {
            MainShopkeepStealChance = BaseShopController.s_mainShopkeepStealChance,
            MainShopkeepItemsStolen = BaseShopController.s_mainShopkeepItemsStolen,
            EmptyFutureShops = BaseShopController.s_emptyFutureShops,
            HasDroppedSerJunkan = Chest.HasDroppedSerJunkanThisSession
        };
    }

    // Token: 0x06007CC7 RID: 31943 RVA: 0x003254F8 File Offset: 0x003236F8
    public static void LoadFromMidGameSave(MidGameStaticShopData ssd)
    {
        BaseShopController.s_mainShopkeepStealChance = ssd.MainShopkeepStealChance;
        BaseShopController.s_mainShopkeepItemsStolen = ssd.MainShopkeepItemsStolen;
        BaseShopController.s_emptyFutureShops = ssd.EmptyFutureShops;
        Chest.HasDroppedSerJunkanThisSession = ssd.HasDroppedSerJunkan;
    }

    // Token: 0x06007CC8 RID: 31944 RVA: 0x00325528 File Offset: 0x00323728
    public static void ClearStaticMemory()
    {
        BaseShopController.s_mainShopkeepItemsStolen = 0;
        BaseShopController.s_mainShopkeepStealChance = 1f;
        BaseShopController.s_emptyFutureShops = false;
    }

    // Token: 0x04007FA3 RID: 32675
    private const bool c_allowShopkeepBossFloor = false;

    // Token: 0x04007FA4 RID: 32676
    public BaseShopController.AdditionalShopType baseShopType;

    // Token: 0x04007FA5 RID: 32677
    public bool FoyerMetaShopForcedTiers;

    // Token: 0x04007FA6 RID: 32678
    public bool IsBeetleMerchant;

    // Token: 0x04007FA7 RID: 32679
    public GameObject ExampleBlueprintPrefab;

    // Token: 0x04007FA8 RID: 32680
    [Header("Spawn Group 1")]
    public GenericLootTable shopItems;

    // Token: 0x04007FA9 RID: 32681
    public Transform[] spawnPositions;

    // Token: 0x04007FAA RID: 32682
    [Header("Spawn Group 2")]
    public GenericLootTable shopItemsGroup2;

    // Token: 0x04007FAB RID: 32683
    public Transform[] spawnPositionsGroup2;

    // Token: 0x04007FAC RID: 32684
    public float spawnGroupTwoItem1Chance = 0.5f;

    // Token: 0x04007FAD RID: 32685
    public float spawnGroupTwoItem2Chance = 0.5f;

    // Token: 0x04007FAE RID: 32686
    public float spawnGroupTwoItem3Chance = 0.5f;

    // Token: 0x04007FAF RID: 32687
    [Header("Other Settings")]
    public PlayMakerFSM shopkeepFSM;

    // Token: 0x04007FB0 RID: 32688
    public GameObject shopItemShadowPrefab;

    // Token: 0x04007FB1 RID: 32689
    public GameObject cat;

    // Token: 0x04007FB2 RID: 32690
    public GameObject OptionalMinimapIcon;

    // Token: 0x04007FB3 RID: 32691
    public float ShopCostModifier = 1f;

    // Token: 0x04007FB4 RID: 32692
    [LongEnum]
    public GungeonFlags FlagToSetOnEncounter;

    // Token: 0x04007FB5 RID: 32693
    private OverridableBool m_capableOfBeingStolenFrom = new OverridableBool(false);

    // Token: 0x04007FB6 RID: 32694
    private int m_numberThingsPurchased;

    // Token: 0x04007FB7 RID: 32695
    private static bool m_hasLockedShopProcedurally;

    // Token: 0x04007FB8 RID: 32696
    private bool m_hasBeenEntered;

    // Token: 0x04007FB9 RID: 32697
    private int m_numberOfFirstTypeItems;

    // Token: 0x04007FBA RID: 32698
    protected bool PreventTeleportingPlayerAway;

    // Token: 0x04007FBB RID: 32699
    protected List<GameObject> m_shopItems;

    // Token: 0x04007FBC RID: 32700
    protected List<ShopItemController> m_itemControllers;

    // Token: 0x04007FBD RID: 32701
    protected RoomHandler m_room;

    // Token: 0x04007FBE RID: 32702
    protected TalkDoerLite m_shopkeep;

    // Token: 0x04007FBF RID: 32703
    private FakeGameActorEffectHandler m_fakeEffectHandler;

    // Token: 0x04007FC0 RID: 32704
    [NonSerialized]
    public bool BeetleExhausted;

    // Token: 0x04007FC1 RID: 32705
    private bool m_onLastStockBeetle;

    // Token: 0x04007FC2 RID: 32706
    protected BaseShopController.ShopState m_state;

    // Token: 0x04007FC3 RID: 32707
    protected bool firstTime = true;

    // Token: 0x04007FC4 RID: 32708
    protected int m_hitCount;

    // Token: 0x04007FC5 RID: 32709
    protected float m_timeSinceLastHit = 10f;

    // Token: 0x04007FC6 RID: 32710
    protected float m_preTeleportTimer;

    // Token: 0x04007FC7 RID: 32711
    protected bool m_wasCaughtStealing;

    // Token: 0x04007FC8 RID: 32712
    private float m_stealChance = 1f;

    // Token: 0x04007FC9 RID: 32713
    private int m_itemsStolen;

    // Token: 0x04007FCA RID: 32714
    private static float s_mainShopkeepStealChance = 1f;

    // Token: 0x04007FCB RID: 32715
    private static int s_mainShopkeepItemsStolen;

    // Token: 0x04007FCC RID: 32716
    private static bool s_emptyFutureShops;

    // Token: 0x02001549 RID: 5449
    public enum AdditionalShopType
    {
        // Token: 0x04007FCF RID: 32719
        NONE,
        // Token: 0x04007FD0 RID: 32720
        GOOP,
        // Token: 0x04007FD1 RID: 32721
        BLANK,
        // Token: 0x04007FD2 RID: 32722
        KEY,
        // Token: 0x04007FD3 RID: 32723
        CURSE,
        // Token: 0x04007FD4 RID: 32724
        TRUCK,
        // Token: 0x04007FD5 RID: 32725
        FOYER_META,
        // Token: 0x04007FD6 RID: 32726
        BLACKSMITH,
        // Token: 0x04007FD7 RID: 32727
        RESRAT_SHORTCUT
    }

    // Token: 0x0200154A RID: 5450
    protected enum ShopState
    {
        // Token: 0x04007FD9 RID: 32729
        Default,
        // Token: 0x04007FDA RID: 32730
        GunDrawn,
        // Token: 0x04007FDB RID: 32731
        Hostile,
        // Token: 0x04007FDC RID: 32732
        PreTeleportAway,
        // Token: 0x04007FDD RID: 32733
        TeleportAway,
        // Token: 0x04007FDE RID: 32734
        Gone,
        // Token: 0x04007FDF RID: 32735
        RefuseService
    }
}
