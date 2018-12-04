import nltk
import string
import os
from nltk.corpus import stopwords
from nltk.tokenize import word_tokenize
import glob
import json
from nltk.stem import PorterStemmer
from sklearn.feature_extraction.text import TfidfVectorizer
from pathlib import Path

path = 'Dropbox/Statistics/Arbetsformedlingen/data/'
token_dict = {}
ps = PorterStemmer()

def tokenize(text):
    return [ps.stem(token) for token in nltk.word_tokenize(text)]

for filename in glob.iglob(os.path.join(Path.home(), path, '*.json')):
    with open(filename) as file:
        json_value = json.loads(file.read())
        annonstext = json_value['platsannons']['annons']['annonstext']
        token_dict[filename] = annonstext.lower()
all_stopwords = set( stopwords.words('swedish')) | set (stopwords.words('english'))
#print (list(all_stopwords))
tfidf = TfidfVectorizer(tokenizer=tokenize, stop_words=None)

tfs = tfidf.fit_transform(token_dict.values())

print (tfs)
feature_names = tfidf.get_feature_names()
feature_names_weight = [ (feature_names[col], tfs[0, col]) for col in tfs.nonzero()[1] ]
feature_names_filtered = [ feature_name_weight for feature_name_weight in feature_names_weight if feature_name_weight[1] > 0.0001 and not feature_names_weight[0] in all_stopwords ]
with open('./tfs.json', 'w+') as file:
    file.write(json.dumps(feature_names_filtered))
