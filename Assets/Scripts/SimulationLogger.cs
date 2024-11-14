using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class SimulationLogger : MonoBehaviour
{
    public int numberOfSimulations = 100;
    public CaseOpening caseOpening;
    private StringBuilder _logBuilder;
    
    private static readonly Dictionary<string, int> RarityOrder = new Dictionary<string, int>
    {
        {"MIL_SPEC", 1},
        {"RESTRICTED", 2},
        {"CLASSIFIED", 3},
        {"COVERT", 4},
        {"SPECIAL", 5}
    };

    private void Start()
    {
        // Initialize the StringBuilder to collect log data
        _logBuilder = new StringBuilder();
        
    }

    [ContextMenu("Run Case Simulation")]
    private void RunSimulation()
    {
        Dictionary<ItemData, int> itemCounts = new Dictionary<ItemData, int>();
        Dictionary<string, int> rarityCounts = new Dictionary<string, int>();

        for (int i = 0; i < numberOfSimulations; i++)
        {
            // Get a random item based on bias
            ItemData selectedItem = caseOpening.GetRandomItemByPercentage();

            // Count occurrences of each item
            if (selectedItem != null)
            {
                if (itemCounts.ContainsKey(selectedItem))
                {
                    itemCounts[selectedItem]++;
                    rarityCounts[selectedItem.rarity]++;
                }
                else
                {
                    itemCounts[selectedItem] = 1;
                    rarityCounts[selectedItem.rarity] = 1;
                }
            }
        }

        // Log the results to the StringBuilder instead of Debug.Log
        _logBuilder.AppendLine($"Simulation of {numberOfSimulations} case openings completed.\n");
        itemCounts = itemCounts.OrderBy(item =>
                RarityOrder.ContainsKey(item.Key.rarity) ? RarityOrder[item.Key.rarity] : int.MaxValue)
            .ToDictionary(data => data.Key, data => data.Value);
        foreach (var entry in itemCounts)
        {
            float percentage = (float)entry.Value / numberOfSimulations * 100;
            _logBuilder.AppendLine($"ID: {entry.Key.id} || Gun: {entry.Key.gun} || Name: {entry.Key.name} || Cond: {entry.Key.condition} {(entry.Key.isStatTrak ? "ST" : "")} || Rarity: {entry.Key.rarity}");
            _logBuilder.AppendLine($"Appeared: {entry.Value} times ({percentage:F5}%)\n");
        }
        rarityCounts = rarityCounts.OrderBy(item =>
                RarityOrder.ContainsKey(item.Key) ? RarityOrder[item.Key] : int.MaxValue)
            .ToDictionary(data => data.Key, data => data.Value);
        foreach (var entry in rarityCounts)
        {
            float percentage = (float)entry.Value / numberOfSimulations * 100;
            _logBuilder.AppendLine($"Condition: {entry.Key}");
            _logBuilder.AppendLine($"Appeared: {entry.Value} times ({percentage:F5}%)\n");
        }

        
        SaveLogToFile();
    }

    private void SaveLogToFile()
    {
        string simulationPath = Path.Combine(Application.persistentDataPath, "SimulationResults");
        if (!Directory.Exists(simulationPath)) Directory.CreateDirectory(simulationPath);
        
        string fileName = $"{DateTime.Now} SIM - {numberOfSimulations}x.txt";
        string filePath = Path.Combine(simulationPath, fileName);
        
        
        File.WriteAllText(filePath, _logBuilder.ToString());
        Debug.Log($"Simulation results saved to: {filePath}");
    }
}