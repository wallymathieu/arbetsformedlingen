module Arbetsformedlingen.Text
open System
let alias =[ 
              ["developer"; "swutvecklare";  "utvecklare"; ], "_developer_"
              ["programming"; "utveckling"; "programmering"; "development" ], "_programming_"
              ["services"; "service" ], "_services_"
              ["node js"; "node javascript"; "nodejs"], "_nodejs_"
              ["js"; "javascript" ], "_javascript_"
              ["linux"; "_linux_miljö"], "_linux_"
              ["windows"; "_windows_miljö"; ], "_windows_"
              ["frontend"; "front-end"; "front end"], "_frontend_"
              ["backend"; "back-end"; "back end"], "_backend_"
              ["fullstack"; "full-stack"; "full stack"], "_fullstack_"
              ["r & d"; ], "_research_and_development_"
              ["c ++"; "c++11"; "c++14"], "_c++_"
              ["c #"; "csharp"], "_c#_"
              ["f #"; "fsharp"], "_f#_"
              [".net";], "_dotnet_"
             ]
             //|> List.map (fun (alias,to')->set alias,to')
let mapAlias (text:string)=
    let mutable res =text
    for (list,to') in alias do
      for v in list do
        res <- res.Replace(v,to')
    res

let postfixWords = ["_developer_"; "_programming_"; "_service_"]
let insertPostFix (text:string)=
    let pickPostfix (postfix :string)= 
      let index = text.IndexOf postfix
      if index >0 then Some (postfix,index) else None
    match postfixWords |> List.tryPick pickPostfix with
    | Some (_,index)-> text.Insert(index, "-")
    | None -> text
let normalize = mapAlias >> insertPostFix

let whitespaceChars = [
                  '\r'
                  '\n'
                  ' '
                  '\t'
                  '\u00A0'
                  ]
let punctuationChars = [
                  ';'
                  ':'
                  '.'
                  '!'
                  ','
                  '?'
                  ]
let andChars = ['&']
let otherChars = ['/';')';'('; '•';'·';'\'';'"';'’';'-';'_';'*';'–']
let splitChars = whitespaceChars @ punctuationChars @ andChars @ otherChars |> List.toArray
let splitOnChars (text:string)=text.Split(splitChars, StringSplitOptions.RemoveEmptyEntries)