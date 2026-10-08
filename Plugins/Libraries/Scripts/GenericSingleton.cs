// Created By   :   Isaac Bustad
// Created      :   7/24/2026


using UnityEngine;

public class GenericSingleton<T> where T : class, new()
{
    #region Vars
    protected static T instance = null;
    #endregion Vars



    #region Methods


    #endregion Methods




    #region Constructors
    protected GenericSingleton()
    {
        
    }

    #endregion Constructors




    #region Accessors
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new T();
            }

            return instance;
        }
        
    }
    #endregion Accessors
}
