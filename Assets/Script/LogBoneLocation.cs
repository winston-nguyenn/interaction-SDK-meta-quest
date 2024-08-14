using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Oculus.Interaction.Input;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;

 public class LogBoneLocation : MonoBehaviour
 {
     [SerializeField]
     private Hand hand;
     private Pose currentPose;
     private HandJointId handJointId = HandJointId.HandIndex3; // TO DO: Change this to your bone.
     private List<string> poseDataList = new List<string>();
    private DateTime startTime;
    private TimeSpan uploadInterval = TimeSpan.FromSeconds(30); // Interval in seconds
    // Start is called before the first frame update
    async void Start()
    {
        // Sign in and Initialize Cloud service
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        // Add header
        poseDataList.Add("Timestamp,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW");
        // Initialize Start time
        startTime = DateTime.Now;
    }

    void Update()
    {
        // Check if hand pose is detected. If so, save it.
        if (hand.GetJointPose(handJointId, out currentPose))
        {
            SavePoseToString(currentPose);
            SavePoseToCloud(currentPose);
        }
        // Check if the interval has passed to save a csv file to Unity Cloud
        if (DateTime.Now - startTime >= uploadInterval)
        {
            startTime = DateTime.Now;   // Reset start time
            SaveCSVToCloud();           // Save and upload CSV file
        }
    }

    void SavePoseToString(Pose pose)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string poseData = $"{timestamp},{pose.position.x},{pose.position.y},{pose.position.z}," +
                              $"{pose.rotation.x},{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}";
        poseDataList.Add(poseData);
    }
    private async void SaveCSVToCloud()
    {
        string endTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"HandIndex3_{endTimeStamp}.csv";
        // string filePath = Path.Combine(Application.persistentDataPath, fileName);
        string csvData = string.Join("\n", poseDataList);
        // File.WriteAllText(filePath, csvData);

        //byte[] fileData = File.ReadAllBytes(filePath);
        byte[] fileData = Encoding.UTF8.GetBytes(csvData);
        try
        {
            await CloudSaveService.Instance.Files.Player.SaveAsync(fileName, fileData);
            Debug.Log("CSV uploaded to cloud successfully.");
        }
       catch (CloudSaveValidationException e)
        {
            Debug.LogError(e);
        }
        catch (CloudSaveRateLimitedException e)
        {
            Debug.LogError(e);
        }
        catch (CloudSaveException e)
        {
            Debug.LogError(e);
        }
    }
    async void SavePoseToCloud(Pose pose)
    {
        var poseData = new Dictionary<string, object>
        {
            { "position", new { x = pose.position.x, y = pose.position.y, z = pose.position.z } },
            { "rotation", new { x = pose.rotation.x, y = pose.rotation.y, z = pose.rotation.z, w = pose.rotation.w } }
        };

        try
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(poseData);
            Debug.Log("Pose saved to cloud successfully.");
        }
        catch (CloudSaveValidationException e)
        {
            Debug.LogError(e);
        }
        catch (CloudSaveRateLimitedException e)
        {
            Debug.LogError(e);
        }
        catch (CloudSaveException e)
        {
            Debug.LogError(e);
        }
    }
 }