using System;
using System.Collections.Generic;
using System.Linq;
using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using UnityEngine;

namespace Catnip.Scripts._Systems.Stamina {
// [0] [1] [2] [3] [4] [5] [6] [7] [8] [9]

[Serializable]
public class StaminaState {
    public const float MaxStaminaWidth = 600f;
    public const float ValueToWidthCoeff = 6f;

    private int maxValue; // 100
    private int currentValue; // 70
    private readonly Dictionary<DebuffType, Tuple<int, DebuffItemUi>> debuffValues = new(); // 20, 10

    public int GetCurrentValue() {
        return currentValue;
    }

    public void UpdateCurrentValue(int diff) {
        currentValue = Mathf.Clamp(0, currentValue + diff, GetCurrentMaxValueWithDebuff());
    }

    public IEnumerable<KeyValuePair<DebuffType, Tuple<int, DebuffItemUi>>> GetDebuffsIterator() {
        return debuffValues.AsEnumerable();
    }

    public void AddDebuff(DebuffType debuffType, DebuffItemUi debuffItemUi) {
        // var previousMaxValue = GetCurrentMaxValueWithDebuff();
        debuffValues.Add(debuffType, new Tuple<int, DebuffItemUi>(1, debuffItemUi));
        // var newMaxValue = GetCurrentMaxValueWithDebuff();
        // UpdateCurrentValueFairly(previousMaxValue, newMaxValue, 1);
    }

    public void UpdateDebuff(DebuffType debuffType, int diff) {
        // var previousMaxValue = GetCurrentMaxValueWithDebuff();
        var oldValue = debuffValues[debuffType];
        debuffValues[debuffType] = new Tuple<int, DebuffItemUi>(oldValue.Item1 + diff, oldValue.Item2);
        currentValue = Mathf.Min(currentValue, GetCurrentMaxValueWithDebuff());

        // var newValue = debuffValues[debuffType];
        // debuffValues.IndexOfItem<Tuple<int, DebuffItemUi>>(newValue);
        // debuffValues.Val
        //
        // float iconX = MaxStaminaWidth - (GetDebuffsWidth() - newValue.Item1 * ValueToWidthCoeff / 2);
        // newValue.Item2.UpdateIconPosition(iconX);
        
        // var newMaxValue = GetCurrentMaxValueWithDebuff();
        // UpdateCurrentValueFairly(previousMaxValue, newMaxValue, diff);
    }

    public float GetDebuffsWidth() {
        return debuffValues.Select(it => it.Value.Item1 * ValueToWidthCoeff).Sum();
    }

    public int GetCurrentMaxValueWithDebuff() => maxValue - debuffValues.Values.Select(it => it.Item1).Sum();
    // public int GetCurrentValueWithDebuff() => currentValue - debuffValues.Values.Select(it => it.Item1).Sum();

    // public void UpdateCurrentValueFairly(int previousMaxValue, int newMaxValue, int diff) {
    //     if (currentValue > maxValue) {
    //         currentValue = maxValue;
    //     } else {
    //         // Если эффективный максимум уменьшился, пропорционально уменьшаем текущее значение
    //         if (diff < 0) {
    //             float percentage = (float)currentValue / previousMaxValue;
    //             currentValue = (int)(newMaxValue * percentage);
    //
    //             if (currentValue > newMaxValue) {
    //                 currentValue = newMaxValue;
    //             }
    //         } else if (diff > 0) {
    //             // Если шкала была полной до увеличения, делаем её снова полной
    //             if (currentValue == previousMaxValue) {
    //                 currentValue = newMaxValue;
    //             }
    //             // Иначе сохраняем абсолютное значение (не пропорционально)
    //             // currentValue остается без изменений
    //         }
    //     }
    // }

    public float GetCurrentValueCoerce() => (float)currentValue / maxValue;

    public List<float> GetDebuffValuesCoerce() => debuffValues.Select(it => (float)it.Key / maxValue).ToList();

    public StaminaState(int maxValue, int currentValue, List<int> debuffValues) {
        this.maxValue = maxValue;
        this.currentValue = currentValue;
        debuffValues.AddRange(debuffValues);
    }
}

public enum DebuffType {
    SomeDebuff,
    SomeDebuff1,
    SomeDebuff2,
}
}