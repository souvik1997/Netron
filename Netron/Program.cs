using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

/* Notes for program
 * 1. General gameplay 
 *  Top-down tron clone
 *  Shooting(?)
 * 2. Controls
 *  Arrow keys + spacebar
 * 3. Interfaces
 *  Drawable, Interactable, FiniteLifetimeable, TronEntity
 * 4. Classes
 *  Grid, TronWall (implements Drawable, Interactable, FiniteLifetimeAble), TronPlayer (holds score + implements Remoteable, Drawable, Interactable), Bullet (implements FiniteLifetimeable, Drawable), 
 * 5. Instance variables
 *  Bullet array, player array
 * 6. Static classes
 *  Communicator      
 * 
 * 
 */


namespace Netron
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
