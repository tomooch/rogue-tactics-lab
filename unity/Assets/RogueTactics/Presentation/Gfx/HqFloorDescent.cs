using System.Collections.Generic;
using UnityEngine;
namespace RogueTactics.Presentation.Gfx
{
    /// <summary>
    /// Going down to the next floor (design/specs/03_DUNGEON_AND_ROOMS.md 1, specs/07 7) without covering the screen:
    /// the stair cell opens a circle of light, light takes each companion in turn, the world turns into the next
    /// floor's look (HqFloorLook) while the party is inside the light, and they land with dust. Display only: the
    /// Executor moves the party and changes Core's floor at the returned beats; this never moves a measured object.
    /// </summary>
    public static class HqFloorDescent
    {
        public struct Beats
        {
            /// <summary>Seconds from Play: the stair lights; each companion is taken (Taken[i]); the look switches;
            /// the party has landed; everything has settled.</summary>
            public float Open;public float[] Taken;public float Switch,Land,End;
        }
        public const float StairSeconds=.5f,Stagger=.12f,SwitchAt=.95f,LandAt=1.25f,EndAt=1.7f;
        public static readonly Color Light=new Color(.62f,.86f,1f);

        /// <param name="partyGrounds">Each companion's feet, in the order they go down.</param>
        public static Beats Play(HqVfx vfx,Camera field,int nextFloor,Vector3 stairGround,IReadOnlyList<Vector3> partyGrounds,IReadOnlyList<Vector3> landingGrounds=null,bool applyLook=true)
        {
            var beats=new Beats{Open=0,Taken=new float[partyGrounds.Count],Switch=SwitchAt,Land=LandAt,End=EndAt};
            if(vfx==null)return beats;
            vfx.CastCircle(stairGround,Light,StairSeconds);
            for(var i=0;i<partyGrounds.Count;i++)
            {
                var at=partyGrounds[i];var when=.3f+i*Stagger;beats.Taken[i]=when+.2f;
                vfx.After(when,()=>vfx.Descend(at,Light));
            }
            if(applyLook)vfx.After(SwitchAt,()=>HqFloorLook.Apply(nextFloor,field));
            for(var i=0;i<partyGrounds.Count;i++){var at=landingGrounds==null?partyGrounds[i]:landingGrounds[i];vfx.After(LandAt+i*.06f,()=>vfx.Land(at,Light));}
            return beats;
        }
    }
}
