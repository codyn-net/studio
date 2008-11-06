require 'serialize/object'

module Cpg
	class Allocation < ::Array
		include Serialize::Object
		property :x, :y, :width, :height
	
		def initialize(ini = nil)
			super(ini == nil ? [0, 0, 1, 1] : ini)
			
			self.x = self[0]
			self.y = self[1]
			self.width = self[2]
			self.height = self[3]
		end
		
		def set_property(name, val)
			ret = super
			
			send("set_#{name}", val.to_f)
			
			ret
		end
	
		def set_x(val)
			self[0] = val
		end
	
		def set_y(val)
			self[1] = val 
		end
	
		def set_width(val)
			self[2] = val 
		end
	
		def set_height(val)
			self[3] = val 
		end
	
		def *(some)
			Allocation.new(map { |x| x * some })
		end
	
		def -(some)
			Allocation.new(map { |x| x - some })
		end
	
		def +(some)
			Allocation.new(map { |x| x + some })
		end
	
		def /(some)
			Allocation.new(map { |x| x / some })
		end
	
		def move(x, y)
			self.x = x
			self.y = y
		end
	
		def move_rel(x, y)
			self.x += x
			self.y += y
		end
	end
end
