using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using UnityEngine;

public class SimulationLogger : MonoBehaviour
{
    public int numberOfSimulations = 100;
    public CaseOpening caseOpening;
    private StringBuilder _logBuilder;
    
    private void Start()
    {
        // Initialize the StringBuilder to collect log data
        _logBuilder = new StringBuilder();
        
    }

    [ContextMenu("Run Case Simulation")]
    private void RunSimulation()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        
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
        
        stopwatch.Stop();

        // Log the results to the StringBuilder instead of Debug.Log
        _logBuilder.AppendLine($"Simulation of {numberOfSimulations} case openings completed.\n");
        _logBuilder.AppendLine($"Simulation Duration: {stopwatch.ElapsedMilliseconds} ms\n");

        itemCounts = itemCounts.OrderBy(item =>
                RarityOrder.RarityOrderList.ContainsKey(item.Key.rarity) ? RarityOrder.RarityOrderList[item.Key.rarity] : int.MaxValue)
            .ToDictionary(data => data.Key, data => data.Value);
        foreach (var entry in itemCounts)
        {
            float percentage = (float)entry.Value / numberOfSimulations * 100;
            _logBuilder.AppendLine($"ID: {entry.Key.id} || Gun: {entry.Key.gun} || Name: {entry.Key.name} || Condition: {entry.Key.condition} {(entry.Key.isStatTrak ? "ST" : "")} || Rarity: {entry.Key.rarity}");
            _logBuilder.AppendLine($"Appeared: {entry.Value} times ({percentage:F5}%)\n");
        }
        rarityCounts = rarityCounts.OrderBy(item =>
                RarityOrder.RarityOrderList.ContainsKey(item.Key) ? RarityOrder.RarityOrderList[item.Key] : int.MaxValue)
            .ToDictionary(data => data.Key, data => data.Value);
        List<float> averagePercentageDifList = new List<float>();
        
        foreach (var entry in rarityCounts)
        {
            float percentage = (float)entry.Value / numberOfSimulations * 100;

            if (RarityWeights.WeightList.TryGetValue(entry.Key, out float weight))
            {
                float expectedPercentage = weight * 100;
                float percentageDif = percentage - expectedPercentage;
                averagePercentageDifList.Add(percentageDif);
                
                _logBuilder.AppendLine($"Condition: {entry.Key}"); 
                _logBuilder.AppendLine($"Appeared: {entry.Value} times ({percentage:F5}%) with a difference of {percentageDif:F5}%\n");
            }
            else
            {
                _logBuilder.AppendLine($"Condition: {entry.Key}");
                _logBuilder.AppendLine($"Appeared: {entry.Value} times ({percentage:F5}%) but no reference value found in RarityWeights.\n");
            }
            
        }

        float averagePercentageDif = averagePercentageDifList.Average();
        _logBuilder.AppendLine($"Average percentage difference: {averagePercentageDif:F5}%");
        SaveLogToFile();
    }

    private void SaveLogToFile()
    {
        string simulationPath = Path.Combine(Application.persistentDataPath, "SimulationResults");
        if (!Directory.Exists(simulationPath)) Directory.CreateDirectory(simulationPath);
        
        string fileName = $"{DateTime.Now} SIM - {numberOfSimulations}x.txt";
        string filePath = Path.Combine(simulationPath, fileName);
        
        File.WriteAllText(filePath, _logBuilder.ToString());
        UnityEngine.Debug.Log($"Simulation results saved to: {filePath}");
    }
}