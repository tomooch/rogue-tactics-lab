using UnityEngine;
namespace RogueTactics.Presentation
{
    public sealed partial class MobileG1View
    {
        public void SetComparisonYaw(float yaw){CameraYaw=yaw;SetComparisonCamera(CameraPitch);}
        public void SetComparisonCamera(float pitch)
        {
            CameraPitch=pitch;var center=new Vector3(2*Cell,0,2*Cell*DepthSpacing+1.1f);
            FieldCamera.transform.position=center+Quaternion.Euler(0,CameraYaw,0)*new Vector3(0,Mathf.Sin(pitch*Mathf.Deg2Rad)*21,-Mathf.Cos(pitch*Mathf.Deg2Rad)*21);FieldCamera.transform.LookAt(center);
            foreach(var r in bodies.Values)if(r.name=="Private approved static skin")
            {r.transform.rotation=FieldCamera.transform.rotation;r.transform.localPosition=Vector3.up*.12f+FieldCamera.transform.up*1.4f;}
            Refresh();
        }
    }
}
