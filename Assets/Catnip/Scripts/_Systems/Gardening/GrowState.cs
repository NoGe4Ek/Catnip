namespace Catnip.Scripts._Systems.Gardening {
public enum GrowState {
    Empty,
    Soiled,  // тип марка почвы, качество
    Watered, // уровень воды > < применимого
    Planted, // сорт мяты (seed), марка, качество, сколько растет
    Grown
}
}
