using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using QuantumCrossy.Input;
using QuantumCrossy.Player;
using QuantumCrossy.Scoring;
using QuantumCrossy.UI;

namespace QuantumCrossy.Core
{
    public sealed class GameFlow : MonoBehaviour
    {
        QuantumCrossyConfig config;
        ScoreDistribution distribution;
        PlayerController player;
        GameHud hud;
        MoveInput moveInput;
        bool restarting;

        public bool IsPlaying { get; private set; }

        public void Begin(
            QuantumCrossyConfig config,
            ScoreDistribution distribution,
            PlayerController player,
            GameHud hud,
            MoveInput moveInput)
        {
            this.config = config;
            this.distribution = distribution;
            this.player = player;
            this.hud = hud;
            this.moveInput = moveInput;
            IsPlaying = true;
        }

        public void Collapse()
        {
            if (!IsPlaying)
                return;

            IsPlaying = false;
            if (moveInput != null)
                moveInput.SetAccepting(false);

            ScoreDistribution.Eigenstate state = distribution.Sample();
            int distance = player != null ? player.MaxForwardRow : 0;
            int score = Mathf.Max(0, Mathf.RoundToInt(state.Scale * distance * config.scorePerDistance));
            if (player != null)
                player.PlayCollapseVisual();
            if (hud != null)
            {
                hud.ShowCollapse(
                    score,
                    state.Name,
                    distribution.HighScoreProbability,
                    distribution.PhotonCount,
                    distance);
            }
        }

        public void Restart()
        {
            if (restarting)
                return;
            restarting = true;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else
                SceneManager.LoadScene(scene.name);
        }

        void Update()
        {
            if (IsPlaying || restarting)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.rKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame
                || keyboard.spaceKey.wasPressedThisFrame)
            {
                Restart();
            }
        }
    }
}
