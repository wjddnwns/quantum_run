using System;
using UnityEngine;

namespace QuantumCrossy.Scoring
{
    /// <summary>
    /// 점수 고유상태의 확률 분포. 광자를 먹을수록 고득점 상태로 기울어진다.
    /// 측정(게이트 접촉) 때 이 분포에서 상태를 하나 뽑는다.
    /// </summary>
    public sealed class ScoreDistribution
    {
        public readonly struct Eigenstate
        {
            public readonly string Name;
            public readonly float Scale;

            public Eigenstate(string name, float scale)
            {
                Name = name;
                Scale = scale;
            }
        }

        static readonly Eigenstate[] States =
        {
            new Eigenstate("바닥", 1f),
            new Eigenstate("낮음", 3f),
            new Eigenstate("중간", 8f),
            new Eigenstate("높음", 20f),
            new Eigenstate("전설", 50f)
        };

        static readonly float[] BaseWeights = { 0.50f, 0.28f, 0.14f, 0.06f, 0.02f };

        readonly float[] probabilities = new float[States.Length];
        readonly float[] weights = new float[States.Length];
        float beta;
        float betaPerPhoton;
        float highScoreMinScale;

        public int PhotonCount { get; private set; }
        public float HighScoreProbability { get; private set; }
        public float HighScoreCutoff => highScoreMinScale;
        public int StateCount => States.Length;
        public event Action Changed;

        public Eigenstate StateAt(int index)
        {
            return States[index];
        }

        public ScoreDistribution(float betaPerPhoton, float highScoreMinScale)
        {
            this.betaPerPhoton = Mathf.Max(0.01f, betaPerPhoton);
            this.highScoreMinScale = highScoreMinScale;
            Recompute();
        }

        public void AddPhotons(int count)
        {
            if (count <= 0)
                return;

            PhotonCount += count;
            beta = Mathf.Min(6f, beta + betaPerPhoton * count);
            Recompute();
            Changed?.Invoke();
        }

        public Eigenstate Sample()
        {
            float roll = UnityEngine.Random.value;
            float cumulative = 0f;
            for (int i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (roll <= cumulative)
                    return States[i];
            }

            return States[States.Length - 1];
        }

        void Recompute()
        {
            float sum = 0f;
            for (int i = 0; i < States.Length; i++)
            {
                weights[i] = BaseWeights[i] * Mathf.Exp(beta * i);
                sum += weights[i];
            }

            if (sum <= 0f)
                sum = 1f;

            float high = 0f;
            for (int i = 0; i < States.Length; i++)
            {
                probabilities[i] = weights[i] / sum;
                if (States[i].Scale >= highScoreMinScale)
                    high += probabilities[i];
            }

            HighScoreProbability = high;
        }
    }
}
