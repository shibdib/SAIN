using EFT;
using EFT.Interactive;
using JetBrains.Annotations;
using SAIN.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Class824;
using static RootMotion.FinalIK.InteractionTrigger.Range;

namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINDoorOpener
    {
        public SAINDoorOpener(SAINComponentClass sain, BotOwner bot)
        {
            SAIN = sain;
            this.BotOwner = bot;
        }

        private SAINComponentClass SAIN;

        public void Init()
        {

        }

        public void Dispose()
        {

        }

        public bool CanOpenDoorNow => _nextPosibleDoorOpenTime > Time.time;

        public bool Update()
        {
            if (ModDetection.ProjectFikaLoaded)
            {
                return BotOwner.DoorOpener.Update();
            }

            List<NavMeshDoorLink> list = this.BotOwner.CellData.CurrentDoorLinks();
            if (BotOwner.DoorOpener.Interacting)
            {
                //this.BotOwner.Steering.SetYAngle(0f);
                if (this._traversingEnd < Time.time)
                {
                    BotOwner.DoorOpener.Interacting = false;
                    BotOwner.Mover.MovementResume();
                }
                return this._lastFrameLink;
            }
            if (this._searchCloseDoorTime < Time.time)
            {
                this.NearDoor = false;
                GStruct18? gstruct = this.method_0(list, null);
                if (gstruct == null)
                {
                    return true;
                }
                this._currLink = gstruct.Value.LinkDoor;
                this._currentDoor = null;
                this._searchCloseDoorTime = Time.time + 0.25f;
                bool lastFrameLink = true;
                if (gstruct.Value.CurDist < 27.039997f)
                {
                    lastFrameLink = this.method_1(this._currLink);
                }
                this._lastFrameLink = lastFrameLink;
                if (gstruct.Value.CurDist < 16f)
                {
                    bool wantToOpen;
                    if (!(wantToOpen = this.CheckWantToOpen(this.BotOwner.Mover.RealDestPoint, gstruct.Value)) && list.Count > 1)
                    {
                        gstruct = this.method_0(list, new GStruct18?(gstruct.Value));
                        if (gstruct == null)
                        {
                            return true;
                        }
                        this._currLink = gstruct.Value.LinkDoor;
                        wantToOpen = this.CheckWantToOpen(this.BotOwner.Mover.RealDestPoint, gstruct.Value);
                    }
                    if (wantToOpen)
                    {
                        if (this._currLink.ShallInteract())
                        {
                            if (!this._shallStartInteract)
                            {
                                this._shallStartInteract = true;
                            }
                            this.TraverseDoorLink();
                        }
                        else if (this._refreshWayPeriod < Time.time)
                        {
                            this._refreshWayPeriod = Time.time + 2f;
                            this.BotOwner.GoToPoint(this.BotOwner.Mover.LastDestination(), false, -1f, false, false, false, true);
                        }
                    }
                }
            }
            return this._lastFrameLink;
        }

        public void TraverseDoorLink()
        {
            this._currentDoor = this._currLink.Door;
            this.TryInteract();
        }

        private IEnumerator InteractWithDoor(Door door, EInteractionType Etype, float delay = 1f)
        {
            BotOwner.DoorOpener.Interacting = true;
            _traversingEnd = Time.time + 0.25f;

            bool inverted = false;
            if (Etype == EInteractionType.Open 
                && ShallInvertDoorAngle(door))
            {
                inverted = true;
                Logger.LogAndNotifyWarning("Angle Inverted");
                door.OpenAngle = -door.OpenAngle;
            }

            InteractionResult interactionResult = new InteractionResult(Etype);
            BotOwner.GetPlayer.CurrentManagedState.StartDoorInteraction(door, interactionResult, null);

            if (inverted)
            {
                yield return new WaitForSeconds(delay);
                if (door != null)
                {
                    Logger.LogAndNotifyWarning("Angle Reset");
                    door.OpenAngle = -door.OpenAngle;
                }
            }
        }

        public void Interact(Door door, EInteractionType Etype)
        {
            EDoorState state = EDoorState.None;
            switch (Etype)
            {
                case EInteractionType.Open:
                    state = EDoorState.Open;
                    break;
                case EInteractionType.Close:
                    state = EDoorState.Shut;
                    break;
                default:
                    break;
            }

            BotOwner.DoorOpener.Interacting = true;
            _traversingEnd = Time.time + 0.25f;

            if (state != EDoorState.None)
            {
                bool inverted = false;
                if (ShallInvertDoorAngle(door))
                {
                    inverted = true;
                    door.OpenAngle = -door.OpenAngle;
                }

                door.method_3(state, false);
                if (inverted)
                {
                    door.OpenAngle = -door.OpenAngle;
                }
            }
            else
            {
               InteractionResult interactionResult = new InteractionResult(Etype);
               BotOwner.GetPlayer.CurrentManagedState.StartDoorInteraction(door, interactionResult, null);
            }
        }

        private bool ShallInvertDoorAngle(Door door)
        {
            var interactionParameters = door.GetInteractionParameters(BotOwner.Position);
            if (interactionParameters.AnimationId == door.PushID)
            {
                return false;
            }
            return true;
        }

        public GStruct18? method_0(List<NavMeshDoorLink> list, [CanBeNull] GStruct18? exclude)
        {
            GStruct18? result = null;
            float num = 6f;
            for (int i = 0; i < list.Count; i++)
            {
                NavMeshDoorLink navMeshDoorLink = list[i];
                Vector3 a;
                if (navMeshDoorLink.Door.DoorState == EDoorState.Open)
                {
                    a = navMeshDoorLink.MidClose;
                }
                else
                {
                    a = navMeshDoorLink.MidOpen;
                }
                if (exclude == null || navMeshDoorLink.Id != exclude.Value.LinkDoor.Id)
                {
                    float sqrMagnitude = (a - this.BotOwner.Transform.position).sqrMagnitude;
                    if (sqrMagnitude < num)
                    {
                        num = sqrMagnitude;
                        this._currLink = navMeshDoorLink;
                        GStruct18 value = new GStruct18
                        {
                            LinkDoor = navMeshDoorLink,
                            CurDist = sqrMagnitude
                        };
                        result = new GStruct18?(value);
                    }
                }
            }
            return result;
        }

        public bool method_1(NavMeshDoorLink link)
        {
            GClass297 gclass = null;
            EDoorState doorState = link.Door.DoorState;
            if (doorState != EDoorState.Shut)
            {
                if (doorState == EDoorState.Open)
                {
                    gclass = link.SegmentClose;
                }
            }
            else
            {
                gclass = link.SegmentOpen;
            }
            if (gclass == null)
            {
                return true;
            }
            Vector3 vector = GClass760.Rotate90(gclass.a - gclass.b, 1);
            if (Vector3.Dot(vector, this.BotOwner.LookDirection) < 0f)
            {
                vector = -vector;
            }
            return !Vector.IsAngLessNormalized(Vector.NormalizeFastSelf(this.BotOwner.LookDirection), Vector.NormalizeFastSelf(vector), 0.9396926f);
        }

        public void TryInteract()
        {
            const float pauseTime = 0.5f;

            float time = Time.time;
            float num = time - this._comeToDoorLast;
            this.BotOwner.Mover.SprintPause(pauseTime * 2f);
            //this.BotOwner.Mover.MovementPause(pauseTime);

            //____owner.MovementPause(this.Single_0__traversingEndFreq);
            if (num > 3f)
            {
                _comeToDoorLast = time;
                return;
            }
            if (!_currentDoor.Operatable || BotOwner.DoorOpener.CanOpenDoorNow)
            {
                return;
            }
            if (_currentDoor.DoorState == EDoorState.Interacting)
            {
                _nextPosibleDoorOpenTime = Time.time + pauseTime;
                return;
            }
            _shallStartInteract = false;
            if (_currentDoor.DoorState != EDoorState.Shut)
            {
                if (_currentDoor.DoorState == EDoorState.Open)
                {
                    //____nextPosibleDoorOpenTime = Time.time + 0.75f;
                    //Interact(____currentDoor, EInteractionType.Close);
                }
                return;
            }
            _nextPosibleDoorOpenTime = Time.time + pauseTime;
            Interact(_currentDoor, EInteractionType.Open);
        }

        // Token: 0x060010AF RID: 4271 RVA: 0x0004CED4 File Offset: 0x0004B0D4
        public bool CheckWantToOpen(Vector3 goTo, GStruct18 infoStruct)
        {
            NavMeshDoorLink linkDoor = infoStruct.LinkDoor;
            Vector3 vector = this.BotOwner.Transform.position + Vector3.up;
            if (Mathf.Abs(vector.y - infoStruct.LinkDoor.Open1.y) >= 0.5f)
            {
                return false;
            }
            this.NearDoor = true;
            if (linkDoor.Door.DoorState == EDoorState.Interacting)
            {
                if (this._nextWaitInteractedDoor < Time.time)
                {
                    this._nextWaitInteractedDoor = Time.time + 3f;
                    this.BotOwner.Mover.SprintPause(1f);
                }
                return false;
            }
            GClass297 gclass = null;
            DoorActionType doorActionType = DoorActionType.none;
            if (linkDoor.Door.DoorState == EDoorState.Open)
            {
                gclass = linkDoor.SegmentClose;
                doorActionType = DoorActionType.wantClose;
                if ((linkDoor.MidClose - vector).sqrMagnitude > 6f)
                {
                    return false;
                }
            }
            if (linkDoor.Door.DoorState == EDoorState.Shut)
            {
                float sqrMagnitude = (linkDoor.MidOpen - vector).sqrMagnitude;
                doorActionType = DoorActionType.wantOpen;
                gclass = linkDoor.SegmentOpen;
                if (sqrMagnitude > 6f)
                {
                    return false;
                }
            }
            if (gclass == null)
            {
                return false;
            }
            DoorActionType doorActionType2 = DoorActionType.none;
            Vector3 v = vector - goTo;
            v.y = 0f;
            Vector3 a = Vector.NormalizeFastSelf(v);
            Vector3 b = a * 0.25f;
            Vector3 b2 = a * 0.5f;
            Vector3 a2 = vector + b;
            Vector3 b3 = goTo - b2;
            if (GClass301.GetCrossPoint(new GClass297(a2, b3), gclass) != null)
            {
                doorActionType2 = doorActionType;
            }
            if (doorActionType2 != DoorActionType.wantClose)
            {
                if (doorActionType2 == DoorActionType.wantOpen)
                {
                    if (linkDoor.Door.DoorState == EDoorState.Shut)
                    {
                        return true;
                    }
                }
            }
            else if (linkDoor.Door.DoorState == EDoorState.Open)
            {
                return true;
            }
            return false;
        }

        public bool NearDoor;
        public bool _lastFrameLink;
        private NavMeshDoorLink _currLink;
        private Door _currentDoor;
        private readonly BotOwner BotOwner;
        private float _nextPosibleDoorOpenTime;
        private float _traversingEnd;
        private float _searchCloseDoorTime;
        private float _comeToDoorLast;
        private float _nextWaitInteractedDoor;
        private bool _shallStartInteract;
        private float _refreshWayPeriod;
    }
}
