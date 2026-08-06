using System;
using System.Collections;
using System.Reflection;

namespace Oink
{
    public static class OinkReflect
    {
        public static object FindGameScreen(object screenManager)
        {
            try
            {
                if (screenManager == null)
                {
                    return null;
                }

                var field = screenManager.GetType().GetField("screens", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    OinkEntry.Log("No screens field on ScreenManager.");
                    return null;
                }

                var list = field.GetValue(screenManager) as IList;
                if (list == null)
                {
                    OinkEntry.Log("No screens field on ScreenManager.");
                    return null;
                }

                foreach (var elem in list)
                {
                    if (elem != null && elem.GetType().FullName.StartsWith("Blood.BloodnBacon"))
                    {
                        return elem;
                    }
                }

                OinkEntry.Log("No BloodnBacon screen active.");
                return null;
            }
            catch (Exception ex)
            {
                OinkEntry.Log("FindGameScreen failed: " + ex.Message);
                return null;
            }
        }

        public static object GetField(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            try
            {
                var field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null)
                {
                    return null;
                }

                return field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        public static void SetField(object target, string name, object value)
        {
            try
            {
                var field = target.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(target, value);
                }
                else
                {
                    OinkEntry.Log("Field not found: " + name);
                }
            }
            catch (Exception ex)
            {
                OinkEntry.Log("Failed to set " + name + ": " + ex.Message);
            }
        }
    }
}
