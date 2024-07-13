using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using TerrariaCells.Common;

namespace TerrariaCells.Content.UI;

[Autoload(Side = ModSide.Client)]
public class InventoryManager : ModSystem
{
    const int INVENTORY_SLOT_COUNT = 4;

    Player player;
    InventoryUiConfiguration config;
    internal UserInterface userInterface;
    internal TCInventory tcInv;
    internal TCInfo tcInfo;

    private GameTime _lastUpdateUiGameTime;
    public override void OnWorldLoad()
    {
        player = Main.LocalPlayer;

        config = (InventoryUiConfiguration)Mod.GetConfig("InventoryUiConfiguration");
        if (config == null)
        {
            Logging.PublicLogger.Error("No config file found!");
            return;
        }
        for (int i = INVENTORY_SLOT_COUNT; i < 10; i++)
            Main.hotbarScale[i] = 0;

        if (!Main.dedServ)
        {
            userInterface = new UserInterface();
            tcInv = new TCInventory();
            tcInv.Activate();
            tcInfo = new TCInfo();
            tcInfo.Activate();
        }
    }

    public override void PreUpdateItems()
    {
        if (player.selectedItem < INVENTORY_SLOT_COUNT) return;

        if (player.selectedItem > INVENTORY_SLOT_COUNT - 1 + (10 - INVENTORY_SLOT_COUNT) / 2)
            player.selectedItem = INVENTORY_SLOT_COUNT - 1;
        else player.selectedItem = 0;
    }

    public override void PreUpdatePlayers()
    {
        if (config == null) return;

        if (config.EnableInventoryLock)
        {
            if (IsInventoryFull())
            {
                player.preventAllItemPickups = true; // TODO: Figure out why this doesn't block item pickups
            }
            else
            {
                player.preventAllItemPickups = false;
            }
        }

    }

    public override void UpdateUI(GameTime gameTime)
    {
        _lastUpdateUiGameTime = gameTime;

        if (userInterface?.CurrentState == null) return;

        userInterface.SetState(Main.playerInventory ? tcInv : tcInfo);
        userInterface.Update(gameTime);
        if (!Main.playerInventory)
            tcInfo.Update();
    }

    private bool IsInventoryFull()
    {
        for (int i = 0; i < INVENTORY_SLOT_COUNT; i++)
        {
            if (player.inventory[i].IsAir)
            {
                return false;
            }
        }
        return true;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        PreUpdatePlayers();

        int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
        if (inventoryIndex == -1) return;

        //layers[inventoryIndex].Active = false;

        LegacyGameInterfaceLayer tcInventoryLayer = new LegacyGameInterfaceLayer(
            "TerrariaCells: Inventory",
            delegate
            {
                if (_lastUpdateUiGameTime != null && userInterface?.CurrentState != null)
                {
                    userInterface.Draw(Main.spriteBatch, _lastUpdateUiGameTime);
                }
                return true;
            },
            InterfaceScaleType.UI
        );

        layers.Insert(inventoryIndex, tcInventoryLayer);
    }
}

class TCInventory : UIState
{
    public override void OnInitialize()
    {
        UIPanel panel = new UIPanel();
        panel.Width.Set(300, 0);
        panel.Height.Set(300, 0);
        Append(panel);

        UIText text = new UIText("Hello world!"); // 1
        panel.Append(text);                       // 2
    }
}

class TCInfo : UIState
{
    private UIPanel panel;
    private UIText text;
    public override void OnInitialize()
    {
        panel = new UIPanel();
        panel.Width.Set(300, 0);
        panel.Height.Set(300, 0);
        Append(panel);
        text = new UIText("");
        panel.Append(text);

    }
    public void Update()
    {
        var money = new Money();
        var plr = Main.LocalPlayer;
        for (int i = 50; i <= 53; i++)
            money.Add(plr.inventory[i].value);
        text = new UIText(money.ToString());
        panel.Recalculate();//Append(text);                      
    }
}

public class Money //ADHD hyperfocus go brrrr
{
    public int Copper = 0;
    public int Silver = 0;
    public int Gold = 0;
    public int Platinum = 0;
    public int TotalValueInCopper => ((Platinum * 100 + Gold) * 100 + Silver) * 100 + Copper;
    public Money() { }
    public Money(int amtCopper, int amtSilver = 0, int amtGold = 0, int amtPlatinum = 0, bool consolidate = true)
    {
        Set(amtCopper, amtSilver, amtGold, amtPlatinum, consolidate);
    }
    public void Add(int amtCopper, int amtSilver = 0, int amtGold = 0, int amtPlatinum = 0, bool consolidate = true)
    {
        Copper += amtCopper;
        Silver += amtSilver;
        Gold += amtGold;
        Platinum += amtPlatinum;
        if (consolidate)
            Consolidate();
    }
    public bool Take(int amtCopper, int amtSilver = 0, int amtGold = 0, int amtPlatinum = 0, bool consolidate = true)
    {
        if (TotalValueInCopper < ((amtPlatinum * 100 + amtGold) * 100 + amtSilver) * 100 + amtCopper)
            return false;

        Platinum -= amtPlatinum;
        if (Platinum < 0)
        {
            Gold += Platinum * 100;
            Platinum = 0;
        }
        Gold -= amtGold;
        if (Gold < 0)
        {
            Silver += Gold * 100;
            Gold = 0;
        }
        Silver -= amtSilver;
        if (Silver < 0)
        {
            Copper += Silver * 100;
            Silver = 0;
        }
        Copper -= amtCopper;
        if (consolidate)
            Consolidate();
        return true;
    }
    public void Remove(int amtCopper, int amtSilver = 0, int amtGold = 0, int amtPlatinum = 0, bool consolidate = true) //Allows negatives
    {
        Platinum -= amtPlatinum;
        if (Platinum < 0)
        {
            Gold += Platinum * 100;
            Platinum = 0;
        }
        Gold -= amtGold;
        if (Gold < 0)
        {
            Silver += Gold * 100;
            Gold = 0;
        }
        Silver -= amtSilver;
        if (Silver < 0)
        {
            Copper += Silver * 100;
            Silver = 0;
        }
        Copper -= amtCopper;
        if (consolidate)
            Consolidate();
    }
    public void Set(int amtCopper, int amtSilver = 0, int amtGold = 0, int amtPlatinum = 0, bool consolidate = true)
    {
        Copper = amtCopper;
        Silver = amtSilver;
        Gold = amtGold;
        Platinum = amtPlatinum;
        if (consolidate)
            Consolidate();
    }
    public void Consolidate()
    {
        Silver += Copper / 100;
        Copper %= 100;

        Gold += Silver / 100;
        Silver %= 100;

        Platinum += Gold / 100;
        Gold %= 100;
    }

    public static Money operator +(Money a) => a;
    public static Money operator +(Money a, Money b) => new Money(a.TotalValueInCopper + b.TotalValueInCopper);
    public static Money operator -(Money a) => new Money(-a.Copper, -a.Silver, -a.Gold, -a.Platinum, false);
    public static Money operator -(Money a, Money b) => a + (-b);
    public static Money operator *(Money a, int b) => new Money(a.Copper * b, a.Silver * b, a.Gold * b, a.Platinum * b, false);
    public static Money operator *(Money a, float b) => new Money((int)(a.TotalValueInCopper * b));
    public static Money operator /(Money a, float b) => a * (1f / b);
    public static bool operator ==(Money a, Money b) => a.TotalValueInCopper == b.TotalValueInCopper;
    public static bool operator !=(Money a, Money b) => a.TotalValueInCopper != b.TotalValueInCopper;
    public static bool operator >(Money a, Money b) => a.TotalValueInCopper > b.TotalValueInCopper;
    public static bool operator <(Money a, Money b) => a.TotalValueInCopper < b.TotalValueInCopper;
    public static bool operator >=(Money a, Money b) => a.TotalValueInCopper >= b.TotalValueInCopper;
    public static bool operator <=(Money a, Money b) => a.TotalValueInCopper <= b.TotalValueInCopper;

    public override string ToString()
    {
        return $"{(Platinum > 0 ? $"{Platinum} Platinum" : "")}, {(Gold > 0 ? $"{Gold} Gold" : "")}, {(Silver > 0 ? $"{Silver} Silver" : "")}, {(Copper > 0 ? $"{Copper} Copper" : "")}";
    }

    public override bool Equals(object obj)
    {
        if (obj is Money money)
            return this == money;
        return false;
    }

    public override int GetHashCode()
    {
        return TotalValueInCopper;
    }
}