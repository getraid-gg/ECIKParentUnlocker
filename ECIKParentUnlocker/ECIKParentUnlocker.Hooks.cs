﻿using Harmony;
using HEdit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace ECIKParentUnlocker
{
    public static class Hooks
    {

    }

    [HarmonyPatch(typeof(MotionIKParentUI), "OpenCategory",
    typeof(int), typeof(HPart.Group.MotionKind))]
    internal static class OpenCategoryPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            HarmonyUtils.DebugLog("Patching HEdit.MotionIKParentUI.OpenCategory(...)");

            var instructionList = new List<CodeInstruction>(instructions);
            var instructionEnumerator = instructions.GetEnumerator();

            var existingBlockLabel = generator.DefineLabel();
            var branchEndLabel = generator.DefineLabel();

            var insertionPoint = -1;
            var branchEndPoint = -1;
            try
            {
                insertionPoint = instructionList.FindLastIndex((instruction) => instruction.opcode == OpCodes.Blt);
                branchEndPoint = instructionList.FindIndex(insertionPoint, (instruction) => instruction.opcode == OpCodes.Callvirt
                    && ((MethodInfo)instruction.operand).Equals(AccessTools.Property(typeof(TMPro.TMP_Text), "text").SetMethod));
            }
            catch (Exception e)
            {
                FileLog.Log($"An exception occurred when finding branch points:\n{e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            }

            if (insertionPoint == -1 || branchEndPoint == -1)
            {
                FileLog.Log("Failed to find a branch point.\n" +
                    $"insertionPoint: {insertionPoint}; branchEndPoint: {branchEndPoint}");
                while (instructionEnumerator.MoveNext())
                {
                    yield return instructionEnumerator.Current;
                }
                yield break;
            }

            // Insert on the instruction after the branch
            insertionPoint++;
            // Branch to the point _after_ the instruction we found
            branchEndPoint++;

            HarmonyUtils.DebugLog($"Insertion at {insertionPoint}");
            HarmonyUtils.DebugLog($"Branch end at {branchEndPoint}");

            var insertionInstruction = instructionList[insertionPoint];
            var branchEndInstruction = instructionList[branchEndPoint];

            while (instructionEnumerator.MoveNext() && instructionEnumerator.Current != insertionInstruction)
            {
                yield return instructionEnumerator.Current;
            }
            // Now the generator is at the point where we want to branch to if we're using
            // one of the hardcoded parentable IK targets (left/right hands/feet)
            // and it's also where we insert our code

            // if (_area >= 4)
            // use new code
            {
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                yield return new CodeInstruction(OpCodes.Blt, existingBlockLabel);

                // _area -= 4
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Ldc_I4_4);
                yield return new CodeInstruction(OpCodes.Sub);
                yield return new CodeInstruction(OpCodes.Starg, 1);

                var ikTargetIndexBuilder = generator.DeclareLocal(typeof(Pose.KinematicCtrl.IKTargetEN));
                ikTargetIndexBuilder.SetLocalSymInfo("ikTargetIndex");
                yield return new CodeInstruction(OpCodes.Ldc_I4_M1);
                yield return new CodeInstruction(OpCodes.Stloc, ikTargetIndexBuilder);

                var menuTitleBuilder = generator.DeclareLocal(typeof(string));
                menuTitleBuilder.SetLocalSymInfo("menuTitle");
                yield return new CodeInstruction(OpCodes.Ldstr, "");
                yield return new CodeInstruction(OpCodes.Stloc, menuTitleBuilder);

                // switch statement to get IK node ID
                var jumpList = Enumerable.Range(0, 9).Select((i) => generator.DefineLabel()).ToArray();
                var ikTargets = new Pose.KinematicCtrl.IKTargetEN[] { Pose.KinematicCtrl.IKTargetEN.Body,
                    Pose.KinematicCtrl.IKTargetEN.LeftShoulder, Pose.KinematicCtrl.IKTargetEN.LeftArmChain,
                    Pose.KinematicCtrl.IKTargetEN.RightShoulder, Pose.KinematicCtrl.IKTargetEN.RightArmChain,
                    Pose.KinematicCtrl.IKTargetEN.LeftThigh, Pose.KinematicCtrl.IKTargetEN.LeftLegChain,
                    Pose.KinematicCtrl.IKTargetEN.RightThigh, Pose.KinematicCtrl.IKTargetEN.RightLegChain };
                var ikMenuTitles = new string[] { "Body IK Setting",
                    "Left shoulder IK Setting", "Left elbow IK Setting",
                    "Right shoulder IK Setting", "Right elbow IK Setting",
                    "Left waist IK Setting", "Left knee IK Setting",
                    "Right waist IK Setting", "Right knee IK Setting"};

                var afterSwitchLabel = generator.DefineLabel();

                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Switch, jumpList);

                for (int i = 0; i < jumpList.Length; i++)
                {
                    var loadTargetIndexInstruction = new CodeInstruction(OpCodes.Ldc_I4, (int)ikTargets[i]);
                    loadTargetIndexInstruction.labels.Add(jumpList[i]);
                    yield return loadTargetIndexInstruction;
                    yield return new CodeInstruction(OpCodes.Stloc, ikTargetIndexBuilder);

                    yield return new CodeInstruction(OpCodes.Ldstr, ikMenuTitles[i]);
                    yield return new CodeInstruction(OpCodes.Stloc, menuTitleBuilder);
                    yield return new CodeInstruction(OpCodes.Br, afterSwitchLabel);
                }

                // this.selectParentArea = ikTargetIndex;
                var afterSwitchInstruction = new CodeInstruction(OpCodes.Ldarg_0);
                afterSwitchInstruction.labels.Add(afterSwitchLabel);
                yield return afterSwitchInstruction;
                yield return new CodeInstruction(OpCodes.Ldloc, ikTargetIndexBuilder);
                yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(MotionIKParentUI), "selectParentArea"));

                // this.textTitle.text = menuTitle;
                // textTitle.text is a property with a setter
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(MotionIKParentUI), "textTitle"));
                yield return new CodeInstruction(OpCodes.Ldloc, menuTitleBuilder);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Property(typeof(TMPro.TMP_Text), "text").SetMethod);

                yield return new CodeInstruction(OpCodes.Br, branchEndLabel);
            }
            // else continue use the existing code
            instructionEnumerator.Current.labels.Add(existingBlockLabel);

            while (instructionEnumerator.Current != branchEndInstruction)
            {
                yield return instructionEnumerator.Current;
                instructionEnumerator.MoveNext();
            }

            instructionEnumerator.Current.labels.Add(branchEndLabel);
            HarmonyUtils.DebugLog($"Branch points to: {instructionEnumerator.Current.opcode.Name}");
            
            do
            {
                yield return instructionEnumerator.Current;
            } while (instructionEnumerator.MoveNext());

            HarmonyUtils.DebugLog("Wrote patch successfully");
        }
    }

    [HarmonyPatch(typeof(MotionIKUI), "InitUI")]
    internal static class InitUIPatch
    {
        private enum IKTargetIndices
        {
            Body = 4,
            LeftShoulder, LeftElbow,
            RightShoulder, RightElbow,
            LeftWaist, LeftKnee,
            RightWaist, RightKnee
        }

        internal static void Postfix(MotionIKUI __instance,
            MotionIKUI.IKUIInfo ___infoRightHand, MotionIKUI.IKUIInfo ___infoBody,
            MotionIKUI.IKUIInfo ___infoLeftShoulder, MotionIKUI.IKUIInfo ___infoLeftElbow,
            MotionIKUI.IKUIInfo ___infoRightShoulder, MotionIKUI.IKUIInfo ___infoRightElbow,
            MotionIKUI.IKUIInfo ___infoLeftWaist, MotionIKUI.IKUIInfo ___infoLeftKnee,
            MotionIKUI.IKUIInfo ___infoRightWaist, MotionIKUI.IKUIInfo ___infoRightKnee,
            HPart.Group.MotionKind ___kindMotion)
        {
            var motionSettingCanvas = __instance.motionSettingCanvas;
            
            CopyIKParentingButton(___infoRightHand, ___infoBody, IKTargetIndices.Body,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoLeftShoulder, IKTargetIndices.LeftShoulder,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoLeftElbow, IKTargetIndices.LeftElbow,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoRightShoulder, IKTargetIndices.RightShoulder,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoRightElbow, IKTargetIndices.RightElbow,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoLeftWaist, IKTargetIndices.LeftWaist,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoLeftKnee, IKTargetIndices.LeftKnee,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoRightWaist, IKTargetIndices.RightWaist,
                motionSettingCanvas, ___kindMotion);
            CopyIKParentingButton(___infoRightHand, ___infoRightKnee, IKTargetIndices.RightKnee,
                motionSettingCanvas, ___kindMotion);
        }

        private static void CopyIKParentingButton(MotionIKUI.IKUIInfo source, MotionIKUI.IKUIInfo target,
            IKTargetIndices category, MotionSettingCanvas motionSettingCanvas, HPart.Group.MotionKind motionKind)
        {
            var ikButton = GameObject.Instantiate<GameObject>(source.btnIKSelectArea.gameObject).GetComponent<Button>();
            ikButton.transform.SetParent(target.tglUse.transform.parent, false);
            ikButton.OnClickAsObservable().Subscribe(_ => motionSettingCanvas.motionIKParentUI.OpenCategory((int)category, motionKind));
            target.btnIKSelectArea = ikButton;

            var buttonText = GameObject.Instantiate<GameObject>(source.textParent.gameObject).GetComponent<TextMeshProUGUI>();
            buttonText.transform.SetParent(target.btnIKSelectArea.transform, false);
            buttonText.text = "-";
            target.textParent = buttonText;
        }
    }
}
