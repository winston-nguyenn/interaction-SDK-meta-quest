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

 public class RightHandTracking : MonoBehaviour
 {
    [SerializeField]
    private Hand hand;
    [SerializeField]
    private List<GameObject> cubes; // List of cube objects to track
    private Pose currentPose;
    private List<HandJointId> handJointIds;
    private List<string> poseDataList = new List<string>();
    private DateTime startTime;
    private TimeSpan uploadInterval = TimeSpan.FromSeconds(30); // Interval in seconds
    // Start is called before the first frame update
    async void Start()
    {
        // Sign in and Initialize Cloud service
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // Initialize the list of hand joints
        handJointIds = new List<HandJointId>((HandJointId[])System.Enum.GetValues(typeof(HandJointId)));

        // Add header
        poseDataList.Add("Timestamp (yyyyMMdd_HHmmss.ffff),Object,Joint,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW");

        // Initialize Start time
        startTime = DateTime.Now;
    }

    void Update()
    {
        // Loop through all joints: if the hand joint is detected, save tracking data of the joint to a string
        foreach (HandJointId jointId in handJointIds)
        {
            if (hand.GetJointPose(jointId, out currentPose))
            {
                SaveJointPoseToString("Right Hand", jointId, currentPose);
            }
        }
        // Track cube positions
        TrackCubePositions();
        // Check if the interval has passed to save a csv file to Unity Cloud
        if (DateTime.Now - startTime >= uploadInterval)
        {
            startTime = DateTime.Now;   // Reset start time
            SaveCSVToCloud();           // Save and upload CSV file
        }
    }
    void TrackCubePositions()
    {
        foreach (GameObject cube in cubes)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss.ffff");
            Vector3 position = cube.transform.position;
            Quaternion rotation = cube.transform.rotation;
            SaveCubePoseToString("Cube", cube.name, new Pose(position, rotation));
        }
    }
    void SaveCubePoseToString(string objectLabel, string cubeName, Pose pose)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss.ffff");
        string poseData = $"{timestamp},{objectLabel},{cubeName},{pose.position.x},{pose.position.y},{pose.position.z}," +
                          $"{pose.rotation.x},{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}"; 
        poseDataList.Add(poseData);
    }
    void SaveJointPoseToString(string objectLabel, HandJointId jointId, Pose pose)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss.ffff");
        string poseData = $"{timestamp},{objectLabel},{jointId},{pose.position.x},{pose.position.y},{pose.position.z}," +
                          $"{pose.rotation.x},{pose.rotation.y},{pose.rotation.z},{pose.rotation.w}"; 
        poseDataList.Add(poseData);
    }
    private async void SaveCSVToCloud()
    {
        // File name format
        string endTimeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"RightHandJoints_{endTimeStamp}.csv";

        // Convert all hand tracking data to a string
        string csvData = string.Join("\n", poseDataList);
        byte[] fileData = Encoding.UTF8.GetBytes(csvData);

        // Save hand tracking data to Unity Cloud
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