//Copyright 2019 Volodymyr Podshyvalov
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;

namespace ConsoleDraw
{
    /// <summary>
    /// Class that provides API to drawing of primitives in console.
    /// </summary>
    public static class Primitives
    {
        /// <summary>
        /// Drawing line from selected char.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="length"></param>
        public static void DrawLine(char element = '=', int length = 80)
        {
            for (int i = 0; i < length; i++)
                Console.Write(element);
        }

        /// <summary>
        /// Drawing line from selected char.
        /// Set space line befor and after separator.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="length"></param>
        public static void DrawSpacedLine(char element = '=', int length = 80)
        {
            Console.WriteLine();
            for (int i = 0; i < length; i++)
                Console.Write(element);
            Console.WriteLine();
        }
    }
}
