using MessageComparer.Engine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MessageComparer.Engine.Helpers
{
    public static class ObjectComparer
    {
        private static int current = 0;
        private const string arrayFix = "([\\}\\[]),([\\s]*[\\}\\]])";

        public static (string, string) GetDiff(object? dA, object? dB, List<KeysConfigData> keysConfig)
        {
            var result = (new StringBuilder(), new StringBuilder());

            result.Item1.AppendLine(new string('\t', 0) + "{");
            result.Item2.AppendLine(new string('\t', 0) + "{");

            var r = GetDiff(dA, dB, 1, keysConfig);

            result.Item1.Append(r.Item1);
            result.Item2.Append(r.Item2);

            result.Item1.AppendLine(new string('\t', 0) + "}");
            result.Item2.AppendLine(new string('\t', 0) + "}");

            var b1 = Regex.Replace(result.Item1.ToString(), arrayFix, "$1$2", RegexOptions.None);
            var b2 = Regex.Replace(result.Item2.ToString(), arrayFix, "$1$2", RegexOptions.Multiline);

            return (b1, b2);
        }

        private static (StringBuilder, StringBuilder) GetDiff(object? dA, object? dB, int tab, List<KeysConfigData> keysConfig)
        {
            var result = (new StringBuilder(), new StringBuilder());

            var proIdx = 0;
            var eA = (IDictionary<string, object>?)dA;
            var eB = (IDictionary<string, object>?)dB;

            var properties = (eA?.Keys ?? new List<string>()).ToList();
            properties = properties.Concat(eB?.Keys ?? new List<string>()).ToList();
            properties = properties.Select(x => x.ToLowerInvariant()).Distinct().OrderBy(x => x).ToList();

            foreach (var p in properties)
            {
                var endpoint = properties.Last() != p ? "," : "";

                var a = eA?.FirstOrDefault(x => x.Key.ToLower() == p);
                var b = eB?.FirstOrDefault(x => x.Key.ToLower() == p);

                IDictionary<string, object>? outA = null;
                IDictionary<string, object>? outB = null;

                if (a?.Value is IDictionary<string, object> || b?.Value is IDictionary<string, object>)
                {
                    if (a != null)
                    {
                        outA = a?.Value as IDictionary<string, object>;
                    }
                    if (b != null)
                    {
                        outB = b?.Value as IDictionary<string, object>;
                    }

                    AppendProp(p, tab, a != null, b != null);
                    AppendSign("{", 1, a != null, b != null);

                    var r = GetDiff(outA, outB, tab, keysConfig);

                    result.Item1.Append(r.Item1);
                    result.Item2.Append(r.Item2);

                    AppendSign("}" + endpoint, -1, a != null, b != null);
                }
                else if ((a?.Value).ParseCollection(out var cA) && (b?.Value).ParseCollection(out var cB) && (cA != null || cB != null))
                {
                    result.Item1.AppendLine(new string('\t', tab) + $"{{ \"count\": {cA?.Count} }},");
                    result.Item2.AppendLine(new string('\t', tab) + $"{{ \"count\": {cB?.Count} }},");

                    current = 0;

                    AppendProp(p, tab, a != null, b != null);
                    AppendSign("[", 1, a != null, b != null);

                    string[] keys;
                    string[] pkeys;
                    var keyError = new StringBuilder();

                    var currentStep = keysConfig.FirstOrDefault(x => x.Parent.ToLower() == p);
                    if (currentStep != null)
                    {                        
                        pkeys = currentStep.Keys.ToLower().Split(';');
                        keys = currentStep.Sorters?.ToLower().Split(';') ?? pkeys;

                        if (keys?.Length == 0 || keys[0] == "")
                        {
                            keyError.AppendLine($"No keys provided when the parent '{p}' is present in configurations");
                        }
                        else
                        {
                            ValidateKeys(keyError, cA, pkeys, "Primary Key", "1");
                            ValidateKeys(keyError, cA, keys, "Sort Key", "1");
                            ValidateKeys(keyError, cB, pkeys, "Primary Key", "2");
                            ValidateKeys(keyError, cB, keys, "Sort Key", "2");
                        }

                        if (keyError.Length > 0)
                        {
                            throw new Exception(keyError.ToString());
                        }
                    }
                    else
                    {
                        keys = (cA ?? cB).First().Keys.ToArray();
                        pkeys = keys.ToArray()[..1];
                    }

                    if (cA != null)
                    {
                        var tempA = cA.OrderByDescending(x => x[pkeys[0]]);
                        foreach (var k in keys)
                        {
                            if (tempA.All(x => x.Any(y => y.Key == k)))
                            {
                                tempA = tempA.ThenByDescending(x => x[k]);
                            }
                        }
                        cA = tempA.ToList();
                    }
                    if (cB != null)
                    {
                        var tempB = cB.OrderBy(x => x[pkeys[0]]);
                        foreach (var k in keys)
                        {
                            if (tempB.All(x => x.Any(y => y.Key == k)))
                            {
                                tempB = tempB.ThenBy(x => x[k]);
                            }
                        }
                        cB = tempB.ToList();
                    }

                    if (cA != null)
                    {
                        do
                        {
                            for (int i = cA.Count - 1; i >= 0; i--)
                            {
                                var itemA = cA[i];

                                var last = cA.Last() == itemA && (cB?.Count == 0);
                                var endBlock = !last ? "," : "";

                                var itemBs = cB?.Where(x => pkeys.All(y => Convert.ToString(x[y]).ToLower() == Convert.ToString(itemA[y]).ToLower())).ToList();
                                var itemB = itemBs?.Select(x => new
                                {
                                    Key = x,
                                    Count = x.Sum(y => (Convert.ToString(y.Value).ToLower() == (itemA.ContainsKey(y.Key) ? Convert.ToString(itemA[y.Key]).ToLower() : "")) ? 1 : 0)
                                }).OrderByDescending(x => x.Count).Select(x => x.Key).FirstOrDefault();

                                if (itemB != null || pkeys.Length == 0)
                                {
                                    current++;

                                    cA.Remove(itemA);
                                    cB?.Remove(itemB);

                                    AppendSign("{", 1, itemA != null, itemB != null);

                                    var r = GetDiff(itemA, itemB, tab, keysConfig);

                                    result.Item1.Append(r.Item1);
                                    result.Item2.Append(r.Item2);

                                    AppendSign("}" + endBlock, -1, itemA != null, itemB != null);
                                }
                            }
                            if (pkeys.Length > 0)
                            {
                                pkeys = pkeys[..^1];
                            }
                        } while (cA.Count > 0);
                    }

                    if (cB?.Count > 0)
                    {
                        foreach (var itemB in cB)
                        {
                            var last = cB.Last() == itemB;
                            var endBlock = !last ? "," : "";

                            current++;

                            AppendSign("{", 1, false, true);

                            var r = GetDiff(null, itemB, tab, keysConfig);

                            result.Item1.Append(r.Item1);
                            result.Item2.Append(r.Item2);

                            AppendSign("}" + endBlock, -1, false, true);
                        }
                    }

                    AppendSign("]" + endpoint, -1, a != null, b != null);
                }
                else
                {
                    proIdx++; //Prevents Json simplifications when null
                    result.Item1.AppendLine(new string('\t', tab) + (a != null ? $"\"{p}\": {"\"" + a?.Value + "\""}" + endpoint : ""));
                    result.Item2.AppendLine(new string('\t', tab) + (b != null ? $"\"{p}\": {"\"" + b?.Value + "\""}" + endpoint : ""));
                }
            }
            return result;

            void AppendProp(string name, int tab, bool a, bool b)
            {
                result.Item1.AppendLine(a ? (new string('\t', tab) + $"\"{name}\":") : "");
                result.Item2.AppendLine(b ? (new string('\t', tab) + $"\"{name}\":") : "");
            }

            void AppendSign(string sign, int tabDiff, bool a, bool b)
            {
                tab += tabDiff < 0 ? tabDiff : 0;

                result.Item1.AppendLine(a ? (new string('\t', tab) + sign) : "");
                result.Item2.AppendLine(b ? (new string('\t', tab) + sign) : "");

                tab += tabDiff > 0 ? tabDiff : 0;
            }

            void ValidateKeys(StringBuilder keyError, List<IDictionary<string, object>> blocks, string[] keys, string keyType, string messageType)
            {
                var block = blocks?.FirstOrDefault();

                if (block != null)
                {
                    foreach (var key in keys)
                    {
                        if (!block.ContainsKey(key.ToLower()))
                        {
                            keyError.AppendLine($"{keyType} '{key}' not found in Message {messageType}");
                        }
                    }
                }
            }
        }

        private static bool ParseCollection(this object value, out List<IDictionary<string, object>> output)
        {
            if (value == null)
            {
                output = null;
                return true;
            }

            output = new List<IDictionary<string, object>>();

            if (value is ICollection<object> valueC)
            {
                foreach (var item in valueC)
                {
                    if (item is IDictionary<string, object>)
                    {
                        var itemD = new Dictionary<string, object>();

                        foreach (var kp in (IDictionary<string, object>)item)
                        {
                            itemD.Add(kp.Key.ToLower(), kp.Value);
                        }

                        output.Add(itemD);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
