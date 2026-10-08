cd /my/project/folder
git submodule add https://github.com/isaacbustad/BugFreeProductionsLibrary.git Assets/Plugins/BugFreeProductionsLibrary



git remote set-url origin git@github.com:isaacbustad/BugFreeProductionsLibrary.git

git submodule update --remote --merge

git pull origin main
git add Assets/Plugins/BugFreeProductionsLibrary
git commit -m "Updated BugFreeProductionsLibrary to latest version"
git push origin main
