using System;

public class Format
{
    // Convert Unity card format to API format
    public static string RoboflowCardToNodeJsCard(string card)
    {
        if (string.IsNullOrEmpty(card) || card.Length < 2)
            throw new ArgumentException("Invalid card format ###" + card);

        string rank = card.Substring(0, card.Length - 1); // all except last char
        char suit = char.ToLower(card[card.Length - 1]);  // last char to lowercase

        // Map Unity ranks to API ranks
        switch (rank)
        {
            case "10": rank = "T"; break;
            case "J": rank = "J"; break;
            case "Q": rank = "Q"; break;
            case "K": rank = "K"; break;
            case "A": rank = "A"; break;
        }

        return rank + suit;
    }
}
