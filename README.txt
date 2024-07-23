***LOGAN'S THIRD PERSON CAMERA***

LogansThirdPersonCamera (LTPC) is a camera system designed for serving full 3D, third person games in Unity. It currently features a fixed-orbit, over the shoulder camera system, but will be expanded in the future to support independent orbit as well.

Setting Up:
The package includes a Main Camera prefab and 2 camera configurations. You can use those, or create your own camera and config from scratch when following these instructions:
* On your camera, add a ThirdPersonCamera.cs component.
* Using the inspector, assign the transform for the entity/character (presumably the player character) you want the camera to follow to the "FollowTransform" field. 
* Using the inspector, assign at least one configuration to the "MyConfigurations" field of the script. If you're not using the included configs, create your own by right-clicking in the inspector, and choosing Create > LogansThirdPersonCamera > CameraConfig. Then set the values and assign this config to the "MyConfigurations" field as stated before.  You can test the positioning in the editor by clicking the vertical elipses button on the top-right of the ThirdPersonCamera.cs component, and selecting "call InitializeCamera()". This will move the camera to the intial position it will have on game start.

Driving the Camera:
The camera does not update itself. You need to use an outside "driving" script (presumably your player script) to update the camera using the UpdateCamera method. Typically you would do this in the LateUpdate of the driving script.
* In the late update of your "driving" script, call the UpdateCamera method after any player logic that would move the player object has been completed for that frame. Pass in a float representing the camera's horizontal input, vertical input, and time delta. The time delta would be Time.deltaTime if you're calling this method in the Update() or LateUpdate().
* In order to switch configs, call the ChangeConfiguration() method, passing in the index of the config you want to switch to. The switching will be smoother, or jerkier depending on the "switching-to" variables in the config asset.
* You do not need to call any other methods for initialization. The camera does this itself.