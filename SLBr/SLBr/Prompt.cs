// Copyright © 2022 SLT World. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be found in the LICENSE file.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SLBr
{
    public class Prompt
    {
        public string Content { get; set; }
        public Visibility ButtonVisibility { get; set; }
        public string ButtonContent { get; set; }
        public string ButtonTag { get; set; }
        public string ButtonToolTip { get; set; }
        public string CloseButtonTag { get; set; }
        public Visibility IconVisibility { get; set; }
        public string IconText { get; set; }
        public string IconRotation { get; set; }
    }
}
