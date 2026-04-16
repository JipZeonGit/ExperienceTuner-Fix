using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using UnityEngine;

// removed assembly attributes
// removed compiler generated classes
namespace SkillTuner
{
	[BepInPlugin("yourname.valheim.skilltuner", "Skill Tuner", "1.0.0")]
	public sealed class SkillTunerPlugin : BaseUnityPlugin
	{
		private static class DeathPenaltyScope
		{
			private static int _depth;

			public static bool Active => _depth > 0;

			public static void Enter()
			{
				_depth++;
			}

			public static void Exit()
			{
				if (_depth > 0)
				{
					_depth--;
				}
			}
		}

		[HarmonyPatch(typeof(Player), "OnDeath")]
		private static class PlayerOnDeathPatch
		{
			private static void Prefix()
			{
				DeathPenaltyScope.Enter();
			}

			private static void Postfix()
			{
				DeathPenaltyScope.Exit();
			}
		}

		[HarmonyPatch(typeof(Skills), "RaiseSkill")]
		private static class RaiseSkillPatch
		{
			private static void Prefix(ref float factor)
			{
				factor *= Mathf.Max(0f, EffectiveExperienceMultiplier);
			}
		}

		[HarmonyPatch(typeof(Skills), "LowerAllSkills", new Type[] { typeof(float) })]
		private static class LowerAllSkillsPatch
		{
			private static void Prefix(ref float factor)
			{
				if (DeathPenaltyScope.Active)
				{
					factor *= Mathf.Max(0f, EffectiveDeathPenaltyMultiplier);
				}
			}
		}

		[HarmonyPatch(typeof(ZNet), "Awake")]
		private static class ZNetAwakePatch
		{
			private static void Postfix(ZNet __instance)
			{
				if (__instance.IsServer())
				{
					ApplyMultipliersFromConfig();
					BroadcastMultipliers();
				}
				else
				{
					ApplyMultipliers(1f, 1f);
				}
			}
		}

		[HarmonyPatch(typeof(ZNet), "OnNewConnection")]
		private static class ZNetOnNewConnectionPatch
		{
			private static void Postfix(ZNet __instance, ZNetPeer peer)
			{
				if (peer?.m_rpc != null)
				{
					peer.m_rpc.Register<float, float>("SkillTuner Sync", RpcReceiveMultipliers);
					if (__instance.IsServer())
					{
						peer.m_rpc.Invoke("SkillTuner Sync", new object[] { EffectiveExperienceMultiplier, EffectiveDeathPenaltyMultiplier });
					}
				}
			}
		}

		private const string PluginGuid = "yourname.valheim.skilltuner";

		private const string PluginName = "Skill Tuner";

		private const string PluginVersion = "1.0.0";

		private const string RpcSyncMultipliers = "SkillTuner Sync";

		internal static ConfigEntry<float>? ExperienceMultiplier;

		internal static ConfigEntry<float>? DeathPenaltyMultiplier;

		private Harmony? _harmony;

		private bool _registeredSettingHandler;

		internal static SkillTunerPlugin? Instance { get; private set; }

		internal static float EffectiveExperienceMultiplier { get; private set; } = 1f;


		internal static float EffectiveDeathPenaltyMultiplier { get; private set; } = 1f;


		private void Awake()
		{
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Expected O, but got Unknown
			Instance = this;
			ExperienceMultiplier = this.Config.Bind<float>("SkillGain", "ExperienceMultiplier", 1f, "Multiplier applied to all skill XP gains. Use values >1 for faster progression, 1 for default, 0 to disable XP gain.");
			DeathPenaltyMultiplier = this.Config.Bind<float>("SkillLoss", "DeathPenaltyMultiplier", 1f, "Multiplier applied to the skill-loss factor on death. 1 keeps the vanilla penalty, 0 removes it entirely, values >1 increase it.");
			this.Config.SettingChanged += OnConfigSettingChanged;
			_registeredSettingHandler = true;
			_harmony = new Harmony("yourname.valheim.skilltuner");
			_harmony.PatchAll();
			this.Logger.LogInfo((object)"Skill Tuner 1.0.0 loaded.");
		}

		private void OnDestroy()
		{
			if (_registeredSettingHandler)
			{
				this.Config.SettingChanged -= OnConfigSettingChanged;
				_registeredSettingHandler = false;
			}
			Harmony? harmony = _harmony;
			if (harmony != null)
			{
				harmony.UnpatchSelf();
			}
			Instance = null;
		}

		private static void OnConfigSettingChanged(object? sender, SettingChangedEventArgs e)
		{
			if ((UnityEngine.Object)(object)ZNet.instance != null && !ZNet.instance.IsServer())
			{
				SkillTunerPlugin? instance = Instance;
				if (instance != null)
				{
					instance.Logger.LogDebug((object)"Ignoring local config change on client; waiting for host sync.");
				}
			}
			else
			{
				ApplyMultipliersFromConfig();
				BroadcastMultipliers();
			}
		}

		private static void ApplyMultipliersFromConfig()
		{
			float exp = ExperienceMultiplier?.Value ?? 1f;
			float death = DeathPenaltyMultiplier?.Value ?? 1f;
			ApplyMultipliers(exp, death);
		}

		private static void ApplyMultipliers(float exp, float death)
		{
			EffectiveExperienceMultiplier = Mathf.Max(0f, exp);
			EffectiveDeathPenaltyMultiplier = Mathf.Max(0f, death);
			SkillTunerPlugin? instance = Instance;
			if (instance != null)
			{
				instance.Logger.LogInfo((object)$"Skill multipliers set -> XP: {EffectiveExperienceMultiplier:0.###}, Death: {EffectiveDeathPenaltyMultiplier:0.###}");
			}
		}

		private static void BroadcastMultipliers()
		{
			if ((UnityEngine.Object)(object)ZNet.instance == null || !ZNet.instance.IsServer())
			{
				return;
			}
			foreach (ZNetPeer connectedPeer in ZNet.instance.GetConnectedPeers())
			{
				if (connectedPeer?.m_rpc != null)
				{
					connectedPeer.m_rpc.Invoke("SkillTuner Sync", new object[] { EffectiveExperienceMultiplier, EffectiveDeathPenaltyMultiplier });
				}
			}
		}

		private static void RpcReceiveMultipliers(ZRpc rpc, float experienceMultiplier, float deathMultiplier)
		{
			ApplyMultipliers(experienceMultiplier, deathMultiplier);
		}
	}
}
