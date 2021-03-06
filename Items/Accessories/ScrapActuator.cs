﻿using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Localization;

namespace WeaponOut.Items.Accessories
{
    public class ScrapActuator : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Scrap Actuator");
            DisplayName.AddTranslation(GameCulture.Chinese, "废弃的促动器");
            DisplayName.AddTranslation(GameCulture.Russian, "Механический Привод");

            Tooltip.SetDefault(
                "Reduces cooldown between dashes\n" +
                "Increases life regen when moving");
            Tooltip.AddTranslation(GameCulture.Chinese, "减少冲刺（例如克苏鲁之盾的冲刺）的冷却时间\n移动时增加生命回复速度");
			Tooltip.AddTranslation(GameCulture.Russian,
				"Сокращает время между рывками\n" +
				"Восстанавливает здоровье при движении");

        }
        public override void SetDefaults()
        {
            item.width = 22;
            item.height = 22;
            item.rare = 5;
            item.accessory = true;
            item.value = Item.sellPrice(0, 1, 0, 0);
            item.expert = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.dashDelay > 1) player.dashDelay --;
            if (Math.Abs(player.velocity.X) > 1.5f)
            {
                player.lifeRegenCount += 2; // healing per 2 seconds
            }
        }
    }
}
