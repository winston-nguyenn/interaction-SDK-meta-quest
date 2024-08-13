 using System.Collections;
 using System.Collections.Generic;
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
    // Start is called before the first frame update
    void Start()
    {
        SetupAndSignIn();
    }

    // This part of the code should be done at the beginning of your game flow (i.e. Main Menu)
    private async void SetupAndSignIn()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    void Update()
    {
        if (hand.GetJointPose(handJointId, out currentPose))
        {
            SavePoseToCloud(currentPose);
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