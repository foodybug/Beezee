using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCard : Card
{
	public enum eType_Event { NONE, Zombie, Item, Health_Plus1,
		Health_Minus1,
	}
	public enum eType_Item { Oil, Gasoline, Board_With_Nails,
		Machete, Grisly_Femur, Golf_Club, Chaninsaw, Can_Of_Soda,
		Candle }

	public eType_Event event9PM;
	public eType_Event event10PM;
	public eType_Event event11PM;
	public eType_Item item;
}
