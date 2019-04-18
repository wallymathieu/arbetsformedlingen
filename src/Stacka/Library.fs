namespace Stacka


/// also known as Annons
type Ad={
  id:string
  title:string
  url:string
  relevans:int
  source:string}

type WithText<'a>=('a*string)

type RawAd = RawAd of string
