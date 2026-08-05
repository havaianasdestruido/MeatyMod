using System;
using System.Reflection;

namespace QuackMenu
{
    public static class BossSpawner
    {
        public static void Spawn(object screenManager, BossDefinition def, QuackConfig config)
        {
            if (screenManager == null || def == null)
            {
                return;
            }

            Type bossType = Type.GetType(def.ClassName);
            if (bossType == null)
            {
                QuackMenuEntry.Log("Boss class not found: " + def.ClassName);
                return;
            }

            object instance = CreateBoss(screenManager, bossType, def);
            if (instance == null)
            {
                QuackMenuEntry.Log("Boss could not be constructed: " + def.ClassName);
                return;
            }

            RegisterBoss(screenManager, instance);
            QuackMenuEntry.Log("Boss spawned: " + def.Name);
        }

        private static object CreateBoss(object screenManager, Type bossType, BossDefinition def)
        {
            try
            {
                ConstructorInfo[] ctors = bossType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var ctor in ctors)
                {
                    object[] args = BuildArgs(screenManager, ctor.GetParameters(), def);
                    if (args != null)
                    {
                        return ctor.Invoke(args);
                    }
                }
            }
            catch (Exception ex)
            {
                QuackMenuEntry.Log("CreateBoss failed for " + def.ClassName + ": " + ex.Message);
            }
            return null;
        }

        private static object[] BuildArgs(object screenManager, ParameterInfo[] ps, BossDefinition def)
        {
            var args = new object[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                Type t = ps[i].ParameterType;
                if (t == typeof(int))
                {
                    args[i] = def.Weight;
                }
                else if (t.FullName == "Blood.ScreenManager")
                {
                    args[i] = screenManager;
                }
                else if (t == typeof(string))
                {
                    args[i] = def.ModelPrefix + i;
                }
                else if (t == typeof(bool))
                {
                    args[i] = false;
                }
                else
                {
                    return null;
                }
            }
            return args;
        }

        private static void RegisterBoss(object screenManager, object instance)
        {
            try
            {
                var field = screenManager.GetType().GetField("bosses", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var list = field.GetValue(screenManager) as System.Collections.IList;
                    if (list != null)
                    {
                        list.Add(instance);
                        return;
                    }
                }
                QuackMenuEntry.Log("No boss list field found; instance left standalone.");
            }
            catch (Exception ex)
            {
                QuackMenuEntry.Log("RegisterBoss failed: " + ex.Message);
            }
        }
    }
}
