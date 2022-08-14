using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;


public class TellMeWhereScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMSelectable Button;
    public TextMesh Display;

    private KMAudio.KMAudioRef Sound;
    private Coroutine Cycle;
    private int Answer, CorrectPosition, CurrentPos, State; //State: 0 = lower, 2 = higher
    private string Correct, Given;
    private bool Active, Solved;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        Display.text = "--";
        Button.OnInteract += delegate
        {
            StartCoroutine(AnimButton());
            Button.AddInteractionPunch();
            ButtonPress();
            Audio.PlaySoundAtTransform("press", Button.transform);
            return false;
        };
        StartCoroutine(Flicker());
    }

    void Calculate()
    {
        Debug.LogFormat("[Tell Me Where #{0}] You activated the module.", _moduleID);
        Correct = "";
        Given = "ERROR";
        string numerals = "!\"#%?'<>@*";
        int shift = (10 - Bomb.GetSerialNumberNumbers().Last()) % 10;
        numerals = numerals.Substring(shift, numerals.Length - shift) + numerals.Substring(0, shift);
        Correct += Bomb.GetBatteryCount() % 10;
        Correct += Bomb.GetBatteryHolderCount() % 10;
        Correct += Bomb.GetIndicators().Count() % 10;
        Correct += Bomb.GetOnIndicators().Count() % 10;
        Correct += Bomb.GetOffIndicators().Count() % 10;
        Correct += Bomb.GetPortCount() % 10;
        Correct += Bomb.GetPortPlateCount() % 10;
        Correct += Bomb.GetSerialNumberNumbers().First();
        Correct += Bomb.GetSerialNumberNumbers().Last();
        Correct += Bomb.GetModuleIDs().Count() % 10;
        State = Rnd.Range(0, 2);
        CorrectPosition = Rnd.Range(0, Correct.Length);
        if (State == 0)
        {
            Given = Correct.Substring(0, CorrectPosition) + ((int.Parse(Correct[CorrectPosition].ToString()) + 9) % 10) + Correct.Substring(CorrectPosition + 1, Correct.Length - CorrectPosition - 1);
            Answer = CorrectPosition + 1;
        }
        else
        {
            Given = Correct.Substring(0, CorrectPosition) + ((int.Parse(Correct[CorrectPosition].ToString()) + 1) % 10) + Correct.Substring(CorrectPosition + 1, Correct.Length - CorrectPosition - 1);
            Answer = CorrectPosition;
        }
        string cache = Given;
        for (int i = 0; i < 10; i++)
            Given = Given.Replace(char.Parse(i.ToString()), numerals[i]);
        Given = Given.Replace('@', '8');
        Debug.LogFormat("[Tell Me Where #{0}] The given sequence is {1}.", _moduleID, Given);
        Debug.LogFormat("[Tell Me Where #{0}] Since the last digit of the serial number is {1}, the decryption key is {2}.", _moduleID, Bomb.GetSerialNumberNumbers().Last(), numerals);
        Debug.LogFormat("[Tell Me Where #{0}] This sequence, decrypted, is {1}.", _moduleID, cache);
        Debug.LogFormat("[Tell Me Where #{0}] Compared against the prime sequence, {1}, the {2} digit has had 1 {3} it.", _moduleID, Correct,
            new string[]{ "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth" }[CorrectPosition],
            State == 0 ? "subtracted from" : "added to");
        Debug.LogFormat("[Tell Me Where #{0}] The button needs to be pressed when the {1} character is on the {2} of the display.",
            _moduleID, new string[] { "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth" }[CorrectPosition],
            State == 0 ? "left" : "right");
    }

    void ButtonPress()
    {
        if (!Solved)
        {
            if (!Active)
            {
                Active = true;
                Sound = Audio.PlaySoundAtTransformWithRef("timer", Button.transform);
                Calculate();
                Cycle = StartCoroutine(CycleText(Given));
            }
            else
            {
                Active = false;
                try
                {
                    Sound.StopSound();
                }
                catch { }
                StopCoroutine(Cycle);
                if (CurrentPos == Answer)
                {
                    Module.HandlePass();
                    Debug.LogFormat("[Tell Me Where #{0}] The button was pressed correctly. Module solved!", _moduleID);
                    Display.text = "GG";
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Button.transform);
                    Solved = true;
                }
                else
                {
                    Module.HandleStrike();
                    string cache = ' ' + Given + ' ';
                    if (CurrentPos != 11)
                        Debug.LogFormat("[Tell Me Where #{0}] The button was pressed incorrectly. Specifically, the button was pressed when the text in curly brackets was on the display: {1}. Strike!", _moduleID,
                            cache.Substring(0, CurrentPos) + '{' + cache.Substring(CurrentPos, 2) + '}' + cache.Substring(CurrentPos + 2, cache.Length - CurrentPos - 2));
                    else
                        Debug.LogFormat("[Tell Me Where #{0}] You pressed the button when there was nothing on the display, which was (obviously) incorrect. Strike!", _moduleID);
                    Display.text = "--";
                    Audio.PlaySoundAtTransform("buzzer", Button.transform);
                }
            }
        }
    }

    private IEnumerator AnimButton()
    {
        for (int i = 0; i < 3; i++)
        {
            Button.transform.localPosition = new Vector3(Button.transform.localPosition.x, Button.transform.localPosition.y - 0.002f, Button.transform.localPosition.z);
            yield return new WaitForSeconds(0.02f);
        }
        for (int i = 0; i < 6; i++)
        {
            Button.transform.localPosition = new Vector3(Button.transform.localPosition.x, Button.transform.localPosition.y + 0.001f, Button.transform.localPosition.z);
            yield return new WaitForSeconds(0.02f);
        }
    }

    private IEnumerator Flicker()
    {
        while (true)
        {
            Display.color = new Color(1f, 0f, 0f, Rnd.Range(0f, 1f));
            yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator CycleText(string input)
    {
        float timer = 0;
        float duration = 0.5f;
        while (true)
        {
            CurrentPos = 0;
            for (int i = 0; i < input.Length + 1; i++)
            {
                CurrentPos = i;
                Display.text = (' ' + input)[i].ToString() + ('ඞ' + input + ' ')[i + 1];
                while (timer < duration)
                {
                    yield return null;
                    timer += Time.deltaTime;
                }
                timer = 0;
            }
            Display.text = "";
            CurrentPos = 11;
            while (timer < duration)
            {
                yield return null;
                timer += Time.deltaTime;
            }
            timer = 0;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} a' to activate the module. Use '!{0} l8' or '!{0} r8' to press the button when the eight character is on the left/right side of the display (use 0 for the tenth character).";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string validcmds = "0123456789";
        if (command.Length != 2 && command != "a")
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        else if (command == "a" && Active)
        {
            yield return "sendtochaterror The module has already been activated!";
            yield break;
        }
        else if (command != "a")
        {
            if ((command[0] != 'l' && command[0] != 'r') || !validcmds.Contains(command[1]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
        }
        yield return null;
        if (command == "a")
            Button.OnInteract();
        else
        {
            if (command[0] == 'l')
                while (CurrentPos != ((int.Parse(command[1].ToString()) + 9) % 10) + 1)
                    yield return "trycancel Button press cancelled (Tell Me Where).";
            else
                while (CurrentPos != (int.Parse(command[1].ToString()) + 9) % 10)
                    yield return "trycancel Button press cancelled (Tell Me Where).";
            Button.OnInteract();
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        if (!Active)
            Button.OnInteract();
        yield return true;
        while (CurrentPos != Answer)
            yield return true;
        Button.OnInteract();
    }
}