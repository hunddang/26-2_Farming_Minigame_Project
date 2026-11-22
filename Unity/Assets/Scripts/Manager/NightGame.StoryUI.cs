using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightCafe
{
    public sealed partial class NightGame
    {
        private Action<int> formChoice;
        private Action<string> formSubmit;
        private int formIndex, formCount;
        private string formValue = "";

        private void Choices(string title, string body, string[] options, Action<int> selected)
        {
            OpenOverlay(Overlay.StoryForm, story.form); story.title.text = title; story.body.text = body;
            formChoice = selected; formSubmit = null; formIndex = 0; formCount = options.Length; story.input.text = "";
            for (int i = 0; i < 6; i++)
            {
                story.choices[i].gameObject.SetActive(i < options.Length);
                if (i < options.Length) story.choiceLabels[i].text = options[i];
            }
            MarkForm();
        }
        private void Numeric(string title, string body, Action<string> submitted)
        {
            Choices(title, body + "\n숫자 키로 입력 · Backspace 지우기", new[] { "입력 확정" }, _ => { });
            formChoice = null; formSubmit = submitted; formValue = ""; story.input.text = "＿  ＿  ＿  ＿";
        }
        private void ConfirmForm(int index)
        {
            if (overlay != Overlay.StoryForm || index < 0 || index >= formCount) return;
            var choose = formChoice; var submit = formSubmit; string value = formValue;
            CloseOverlay();
            if (submit != null) submit(value); else choose?.Invoke(index);
        }
        private void MarkForm()
        {
            for (int i = 0; i < formCount; i++) story.choices[i].GetComponent<CafeButtonStyle>().Select(i == formIndex);
        }
        private void HandleStoryForm(Keyboard keys)
        {
            if (formSubmit != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    Key key = i == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + i - 1);
                    if (keys[key].wasPressedThisFrame && formValue.Length < 8) formValue += i.ToString();
                }
                if (keys.backspaceKey.wasPressedThisFrame && formValue.Length > 0) formValue = formValue.Substring(0, formValue.Length - 1);
                story.input.text = formValue.Length == 0 ? "＿  ＿  ＿  ＿" : formValue;
            }
            else
            {
                if (keys.wKey.wasPressedThisFrame || keys.upArrowKey.wasPressedThisFrame) formIndex = (formIndex + formCount - 1) % formCount;
                if (keys.sKey.wasPressedThisFrame || keys.downArrowKey.wasPressedThisFrame) formIndex = (formIndex + 1) % formCount;
                MarkForm();
            }
            if (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame) ConfirmForm(formIndex);
        }
        private IEnumerator Movie(string caption, float seconds, Action<float> animate = null, bool cctv = false)
        {
            CloseOverlay(); storyBusy = true; story.cinema.SetActive(true); story.cinemaCaption.text = caption;
            story.cinemaFeed.gameObject.SetActive(cctv);
            if(cctv) { story.cinemaFeed.texture=feed;feedCamera.enabled=true; }
            float elapsed = 0;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime; animate?.Invoke(Mathf.Clamp01(elapsed / seconds)); yield return null;
            }
            story.cinema.SetActive(false); storyBusy = false;
            if(cctv) { feedCamera.enabled=false;story.cinemaFeed.texture=null; }
        }
        private IEnumerator FailureMovie()
        {
            if (Day == 4)
                yield return Movie(Fourth.Failure, tuning.cinematicSeconds, amount => World.Player.transform.localScale = new Vector3(1 - amount * 0.5f, 1 + amount * 0.8f, 1));
            else
            {
                CafeWorld.ViewRoom(feedCamera,CafeWorld.RoomLayer(World.Player.PlanPosition));
                yield return Movie(Fifth.Failure + "\n안에서 화면을 두드려도 바깥에서는 잡음만 들린다.", tuning.cinematicSeconds * 2,
                    amount=>World.Player.transform.localScale=Vector3.one*(1+amount*2),true);
                story.memoryActor.position=new Vector2(-17,6);SetLayer(story.memoryActor,8);story.memoryActor.gameObject.SetActive(true);
                CafeWorld.ViewRoom(feedCamera,8);
                sound.PlayOneShot(clatter,0.15f);
                yield return Movie("다음 근무자가 노트를 펼쳤다.\n그는 소리를 대수롭지 않게 넘겼다. 모니터 안의 그림자가 점점 커진다.", tuning.cinematicSeconds * 2,
                    amount=>story.memoryActor.localScale=Vector3.one*(1+amount),true);
                story.memoryActor.gameObject.SetActive(false);story.memoryActor.localScale=Vector3.one;
            }
            ShowEnd(false);
        }
        private IEnumerator HappyMovie()
        {
            story.bandage.SetActive(true);
            foreach(var part in story.spirit.GetComponentsInChildren<SpriteRenderer>())
                if(part.name=="Body" || part.name=="Arm") part.color=CafeUi.Green;
            sound.PlayOneShot(clatter,0.1f);
            yield return Movie($"퇴실 확인이 기록되자 {tuning.victimName}이 본래의 얼굴과 근무복을 되찾았다.\n멈춰 있던 초침이 한 칸 움직였다.", tuning.cinematicSeconds);
            yield return Movie("어머니와의 통화가 겹쳐 들린다.\n미전송 메시지  ‘엄마, 문이 안 열려’\n컵을 건넸다. 이번에는 혼자 남겨 두지 않았다.", tuning.cinematicSeconds * 2);
            World.Player.Teleport(CafeWorld.SecuritySpawn); story.spirit.position = new Vector2(-15, 7); SetLayer(story.spirit, 8);
            story.securityChair.rotation=Quaternion.Euler(0,0,180);
            yield return Movie("카페 밖으로 나온 뒤의 감시실.\n의자가 CCTV 모니터 정면으로 돌아간다. 붕대를 감싼 손의 윤곽이 편안히 내려앉았다.\n초침 소리와 함께 천천히 어두워진다.", tuning.cinematicSeconds * 2,
                amount=>{ story.securityChair.rotation=Quaternion.Euler(0,0,180*(1-amount));
                    foreach(var part in story.spirit.GetComponentsInChildren<SpriteRenderer>()) { var color=part.color;color.a=1-amount;part.color=color; } });
            ShowEnd(true);
        }
        private void RefreshAdvancedResult(bool success)
        {
            view.completeHeader.text = Day == 5 ? "LAST SHIFT COMPLETE" : "SHIFT COMPLETE  /  04";
            view.failureHeader.text = $"INCIDENT LOST  /  {Day:00}";
            view.completeTitle.text = Day == 5 ? "마지막 근무 종료" : "네 번째 밤이 끝났다";
            view.completeBody.text = Day == 5 ? "남은 사람의 퇴실이 확인되었다.\n두 사람은 함께 문을 나섰다." : "낙하자가 그물에 포획되었다.\n감시실의 모니터에 마지막 근무가 표시된다.";
            view.failureTitle.text = Day == 5 ? "화면 안의 근무자" : "천장 위로 사라졌다";
            view.failureBody.text = Day == 5 ? Fifth.Failure + "\n\n시간 정지 이후의 기록과 소지품, 해결 상태를 가지고 재도전한다." : Fourth.Failure;
            view.failureButtons[0].GetComponentInChildren<UnityEngine.UI.Text>().text = Day == 5 && Fifth.Frozen ? "시간 정지 직후로" : "이 밤 다시 시작";
        }
        private void RetryAdvanced()
        {
            if (Day == 4) { LoadShift(4); return; }
            StopAllCoroutines(); storyBusy = false; endingStarted = false; story.cinema.SetActive(false); CloseOverlay();
            Fifth.Retry(); World.Player.Teleport(CafeWorld.SecuritySpawn); World.Player.transform.localScale = Vector3.one;
            story.spirit.position = CafeWorld.RoomCenter(12); SetLayer(story.spirit, 12);
            SyncFinalObjects(); Toast("시간 정지 직후로 돌아왔다. 소지품과 기록, 해결된 설비는 남아 있다.", 6);
        }
    }
}
